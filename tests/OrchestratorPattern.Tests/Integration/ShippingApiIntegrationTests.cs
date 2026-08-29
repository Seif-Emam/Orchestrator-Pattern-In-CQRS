using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Models;
using OrchestratorPattern.Api.Common.Persistence.Seed;
using OrchestratorPattern.Api.Features.Orders.Commands.CreateOrder;
using OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Integration;

public class ShippingApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ShippingApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateShipment_DirectEndpoint_ShouldCreateShipmentSuccessfully()
    {
        // 1. Create Order first
        var createCommand = new CreateOrderCommand(
            DatabaseSeeder.CustomerBobId,
            new List<CreateOrderItemDto>
            {
                new(DatabaseSeeder.ProductPhoneId, 1)
            }
        );
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var order = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>())!.Data!;

        // 2. Direct Create Shipment call
        var shipmentCommand = new CreateShipmentCommand(
            order.OrderId,
            "789 Tech Boulevard, Austin, TX 78701",
            "FedEx"
        );

        var shipmentResponse = await _client.PostAsJsonAsync("/api/shipments", shipmentCommand);
        shipmentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await shipmentResponse.Content.ReadFromJsonAsync<ApiResponse<CreateShipmentResponse>>();
        result!.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(ShipmentStatus.Created);
        result.Data.TrackingNumber.Should().StartWith("FDX-");
    }
}
