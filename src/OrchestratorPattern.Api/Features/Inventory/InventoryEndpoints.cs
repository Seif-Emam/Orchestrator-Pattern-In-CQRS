using OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;
using OrchestratorPattern.Api.Features.Inventory.Queries.GetProductStock;

namespace OrchestratorPattern.Api.Features.Inventory;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        ReserveInventoryEndpoint.Map(app);
        GetProductStockEndpoint.Map(app);
        return app;
    }
}
