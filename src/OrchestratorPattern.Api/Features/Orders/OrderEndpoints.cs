using OrchestratorPattern.Api.Features.Orders.Commands.Checkout;
using OrchestratorPattern.Api.Features.Orders.Commands.CreateOrder;
using OrchestratorPattern.Api.Features.Orders.Queries.GetOrderById;
using OrchestratorPattern.Api.Features.Orders.Queries.GetOrders;
using OrchestratorPattern.Api.Features.Orders.Queries.GetOrderStatus;

namespace OrchestratorPattern.Api.Features.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        CreateOrderEndpoint.Map(app);
        CheckoutEndpoint.Map(app);
        GetOrderByIdEndpoint.Map(app);
        GetOrderStatusEndpoint.Map(app);
        GetOrdersEndpoint.Map(app);
        return app;
    }
}
