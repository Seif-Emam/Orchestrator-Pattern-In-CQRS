using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Models;
using OrchestratorPattern.Api.Common.Persistence.Seed;
using OrchestratorPattern.Api.Features.Orders.Commands.CreateOrder;
using OrchestratorPattern.Api.Features.Orders.Queries.GetOrderById;
using OrchestratorPattern.Api.Features.Orders.Queries.GetOrders;
using OrchestratorPattern.Api.Features.Orders.Queries.GetOrderStatus;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Integration;

public class OrdersApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrdersApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOrders_ShouldReturnPaginatedList()
    {
        // Act
        var response = await _client.GetAsync("/api/orders?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<GetOrdersResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Orders.Should().NotBeNull();
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetOrderById_WhenOrderExists_ShouldReturnOrderDetails()
    {
        // Arrange - create order
        var createCommand = new CreateOrderCommand(
            DatabaseSeeder.CustomerBobId,
            new List<CreateOrderItemDto>
            {
                new(DatabaseSeeder.ProductPhoneId, 1)
            }
        );
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var createdOrder = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>())!.Data!;

        // Act
        var response = await _client.GetAsync($"/api/orders/{createdOrder.OrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<GetOrderByIdResponse>>();
        result!.Data!.OrderId.Should().Be(createdOrder.OrderId);
        result.Data.CustomerId.Should().Be(DatabaseSeeder.CustomerBobId);
        result.Data.CustomerName.Should().Be("Bob Smith");
        result.Data.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrderById_WhenOrderDoesNotExist_ShouldReturn404()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/orders/{nonExistentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("ORDER_NOT_FOUND");
    }

    [Fact]
    public async Task GetOrderStatus_WhenOrderExists_ShouldReturnStatus()
    {
        // Arrange - create order
        var createCommand = new CreateOrderCommand(
            DatabaseSeeder.CustomerAliceId,
            new List<CreateOrderItemDto>
            {
                new(DatabaseSeeder.ProductHeadphonesId, 1)
            }
        );
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var createdOrder = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>())!.Data!;

        // Act
        var response = await _client.GetAsync($"/api/orders/{createdOrder.OrderId}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<GetOrderStatusResponse>>();
        result!.Data!.OrderId.Should().Be(createdOrder.OrderId);
        result.Data.Status.Should().Be(OrderStatus.Pending);
    }
}
