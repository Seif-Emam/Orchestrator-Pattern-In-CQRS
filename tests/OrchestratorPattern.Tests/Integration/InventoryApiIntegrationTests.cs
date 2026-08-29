using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrchestratorPattern.Api.Common.Models;
using OrchestratorPattern.Api.Common.Persistence.Seed;
using OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;
using OrchestratorPattern.Api.Features.Inventory.Queries.GetProductStock;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Integration;

public class InventoryApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InventoryApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProductStock_WhenProductExists_ShouldReturnStockDetails()
    {
        var response = await _client.GetAsync($"/api/inventory/products/{DatabaseSeeder.ProductLaptopId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<GetProductStockResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.ProductId.Should().Be(DatabaseSeeder.ProductLaptopId);
        result.Data.Sku.Should().Be("TECH-LAPTOP-001");
        result.Data.InStock.Should().BeTrue();
    }

    [Fact]
    public async Task ReserveInventory_DirectEndpoint_ShouldDecrementStock()
    {
        var command = new ReserveInventoryCommand(new List<ReserveInventoryItemDto>
        {
            new(DatabaseSeeder.ProductHeadphonesId, 2)
        });

        var response = await _client.PostAsJsonAsync("/api/inventory/reserve", command);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReserveInventoryResponse>>();
        result!.Success.Should().BeTrue();
        result.Data!.ReservedItems.Should().HaveCount(1);
        result.Data.ReservedItems[0].QuantityReserved.Should().Be(2);
    }
}
