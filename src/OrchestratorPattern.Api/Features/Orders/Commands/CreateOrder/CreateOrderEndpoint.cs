using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrchestratorPattern.Api.Common.Models;

namespace OrchestratorPattern.Api.Features.Orders.Commands.CreateOrder;

public static class CreateOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders", async (
            [FromBody] CreateOrderCommand command,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            return Results.Created($"/api/orders/{result.OrderId}", ApiResponse<CreateOrderResponse>.Ok(result, traceId));
        })
        .WithName("CreateOrder")
        .WithTags("Orders")
        .Produces<ApiResponse<CreateOrderResponse>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }
}
