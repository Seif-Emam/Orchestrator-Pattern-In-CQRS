using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Models;
using OrchestratorPattern.Api.Common.Persistence.Seed;
using OrchestratorPattern.Api.Features.Orders.Commands.CreateOrder;
using OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Integration;

public class PaymentsApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PaymentsApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProcessPayment_DirectEndpoint_ShouldProcessPaymentSuccessfully()
    {
        // 1. Create Order first
        var createCommand = new CreateOrderCommand(
            DatabaseSeeder.CustomerAliceId,
            new List<CreateOrderItemDto>
            {
                new(DatabaseSeeder.ProductLaptopId, 1)
            }
        );
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var order = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>())!.Data!;

        // 2. Direct Process Payment call
        var paymentCommand = new ProcessPaymentCommand(
            order.OrderId,
            order.TotalAmount,
            PaymentMethod.CreditCard,
            "4111111111111111"
        );

        var paymentResponse = await _client.PostAsJsonAsync("/api/payments/process", paymentCommand);
        paymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await paymentResponse.Content.ReadFromJsonAsync<ApiResponse<ProcessPaymentResponse>>();
        result!.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(PaymentStatus.Paid);
        result.Data.Amount.Should().Be(order.TotalAmount);
    }
}
