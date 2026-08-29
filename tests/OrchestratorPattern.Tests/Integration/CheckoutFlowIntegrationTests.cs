using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Models;
using OrchestratorPattern.Api.Common.Persistence.Seed;
using OrchestratorPattern.Api.Features.Inventory.Queries.GetProductStock;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout;
using OrchestratorPattern.Api.Features.Orders.Commands.CreateOrder;
using OrchestratorPattern.Api.Features.Orders.Queries.GetOrderById;
using OrchestratorPattern.Api.Features.Orders.Queries.GetOrderStatus;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Integration;

public class CheckoutFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CheckoutFlowIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CheckoutFlow_CompleteHappyPath_ShouldSucceedAndConfirmOrder()
    {
        // 1. Create Order
        var createOrderCommand = new CreateOrderCommand(
            DatabaseSeeder.CustomerAliceId,
            new List<CreateOrderItemDto>
            {
                new(DatabaseSeeder.ProductLaptopId, 1),
                new(DatabaseSeeder.ProductHeadphonesId, 2)
            }
        );

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createOrderCommand);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>();
        createResult.Should().NotBeNull();
        createResult!.Success.Should().BeTrue();
        createResult.Data.Should().NotBeNull();
        createResult.Data!.Status.Should().Be(OrderStatus.Pending);
        createResult.Data.TotalAmount.Should().Be(1499.99m + (2 * 249.99m)); // $1999.97

        var orderId = createResult.Data.OrderId;

        // 2. Checkout Order
        var checkoutRequest = new CheckoutRequestDto(
            PaymentMethod.CreditCard,
            "123 Innovation Drive, Austin, TX 78701",
            "4111111111111111", // Valid card
            "FedEx"
        );

        var checkoutResponse = await _client.PostAsJsonAsync($"/api/orders/{orderId}/checkout", checkoutRequest);
        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkoutResult = await checkoutResponse.Content.ReadFromJsonAsync<ApiResponse<CheckoutResponse>>();
        checkoutResult.Should().NotBeNull();
        checkoutResult!.Success.Should().BeTrue();
        checkoutResult.Data.Should().NotBeNull();
        checkoutResult.Data!.OrderStatus.Should().Be(OrderStatus.Confirmed);
        checkoutResult.Data.Payment.Status.Should().Be(PaymentStatus.Paid);
        checkoutResult.Data.Payment.TransactionId.Should().StartWith("txn_");
        checkoutResult.Data.Shipment.Status.Should().Be(ShipmentStatus.Created);
        checkoutResult.Data.Shipment.TrackingNumber.Should().StartWith("FDX-");

        // 3. Query Order by ID
        var getResponse = await _client.GetAsync($"/api/orders/{orderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<GetOrderByIdResponse>>();
        getResult.Should().NotBeNull();
        getResult!.Data!.Status.Should().Be(OrderStatus.Confirmed);
        getResult.Data.Payment.Should().NotBeNull();
        getResult.Data.Payment!.Status.Should().Be(PaymentStatus.Paid);
        getResult.Data.Shipment.Should().NotBeNull();
        getResult.Data.Shipment!.Status.Should().Be(ShipmentStatus.Created);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidProduct_ShouldReturnNotFound()
    {
        var invalidProductId = Guid.NewGuid();
        var createOrderCommand = new CreateOrderCommand(
            DatabaseSeeder.CustomerAliceId,
            new List<CreateOrderItemDto>
            {
                new(invalidProductId, 1)
            }
        );

        var response = await _client.PostAsJsonAsync("/api/orders", createOrderCommand);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(ErrorCodes.ProductNotFound);
    }

    [Fact]
    public async Task Checkout_WithInsufficientInventory_ShouldFailAndMarkOrderFailed()
    {
        // 1. Create order requesting out of stock item
        var createOrderCommand = new CreateOrderCommand(
            DatabaseSeeder.CustomerBobId,
            new List<CreateOrderItemDto>
            {
                new(DatabaseSeeder.ProductOutOfStockId, 1)
            }
        );

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createOrderCommand);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>();
        var orderId = createResult!.Data!.OrderId;

        // 2. Checkout
        var checkoutRequest = new CheckoutRequestDto(
            PaymentMethod.CreditCard,
            "456 Main St, Dallas, TX 75001",
            "4111111111111111"
        );

        var checkoutResponse = await _client.PostAsJsonAsync($"/api/orders/{orderId}/checkout", checkoutRequest);
        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errorResult = await checkoutResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        errorResult!.Success.Should().BeFalse();
        errorResult.Error!.Code.Should().Be(ErrorCodes.ProductOutOfStock);

        // 3. Verify order status is marked Failed
        var statusResponse = await _client.GetAsync($"/api/orders/{orderId}/status");
        var statusResult = await statusResponse.Content.ReadFromJsonAsync<ApiResponse<GetOrderStatusResponse>>();
        statusResult!.Data!.Status.Should().Be(OrderStatus.Failed);
    }

    [Fact]
    public async Task Checkout_WithPaymentDecline_ShouldFailAndCompensateInventory()
    {
        // 1. Check initial phone stock
        var stockBeforeResponse = await _client.GetAsync($"/api/inventory/products/{DatabaseSeeder.ProductPhoneId}");
        var stockBefore = (await stockBeforeResponse.Content.ReadFromJsonAsync<ApiResponse<GetProductStockResponse>>())!.Data!.StockQuantity;

        // 2. Create order for 2 phones
        var createOrderCommand = new CreateOrderCommand(
            DatabaseSeeder.CustomerAliceId,
            new List<CreateOrderItemDto>
            {
                new(DatabaseSeeder.ProductPhoneId, 2)
            }
        );

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createOrderCommand);
        var orderId = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>())!.Data!.OrderId;

        // 3. Checkout with declining card (ends with 0000)
        var checkoutRequest = new CheckoutRequestDto(
            PaymentMethod.CreditCard,
            "123 Innovation Drive, Austin, TX 78701",
            "4000000000000000" // Declining card
        );

        var checkoutResponse = await _client.PostAsJsonAsync($"/api/orders/{orderId}/checkout", checkoutRequest);
        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errorResult = await checkoutResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        errorResult!.Success.Should().BeFalse();
        errorResult.Error!.Code.Should().Be(ErrorCodes.PaymentDeclined);

        // 4. Verify order is marked Failed
        var statusResponse = await _client.GetAsync($"/api/orders/{orderId}/status");
        var statusResult = await statusResponse.Content.ReadFromJsonAsync<ApiResponse<GetOrderStatusResponse>>();
        statusResult!.Data!.Status.Should().Be(OrderStatus.Failed);

        // 5. Verify inventory was compensated / released back
        var stockAfterResponse = await _client.GetAsync($"/api/inventory/products/{DatabaseSeeder.ProductPhoneId}");
        var stockAfter = (await stockAfterResponse.Content.ReadFromJsonAsync<ApiResponse<GetProductStockResponse>>())!.Data!.StockQuantity;
        stockAfter.Should().Be(stockBefore);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidRequest_ShouldReturn400WithValidationErrorDetails()
    {
        var invalidCommand = new CreateOrderCommand(
            Guid.Empty, // Empty customer ID
            new List<CreateOrderItemDto>() // Empty items
        );

        var response = await _client.PostAsJsonAsync("/api/orders", invalidCommand);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(ErrorCodes.ValidationError);
        result.Error.Details.Should().NotBeEmpty();
    }
}
