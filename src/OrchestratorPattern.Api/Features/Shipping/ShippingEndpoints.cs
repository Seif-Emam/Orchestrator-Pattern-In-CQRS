using OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;

namespace OrchestratorPattern.Api.Features.Shipping;

public static class ShippingEndpoints
{
    public static IEndpointRouteBuilder MapShippingEndpoints(this IEndpointRouteBuilder app)
    {
        CreateShipmentEndpoint.Map(app);
        return app;
    }
}
