using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Models;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout;

public record CheckoutRequestDto(
    PaymentMethod PaymentMethod,
    string ShippingAddress,
    string? CardNumber = null,
    string Carrier = "FedEx");

public static class CheckoutEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/{id:guid}/checkout", async (
            [FromRoute] Guid id,
            [FromBody] CheckoutRequestDto request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var command = new CheckoutCommand(
                id,
                request.PaymentMethod,
                request.ShippingAddress,
                request.CardNumber,
                request.Carrier
            );

            var result = await sender.Send(command, cancellationToken);
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            return Results.Ok(ApiResponse<CheckoutResponse>.Ok(result, traceId));
        })
        .WithName("CheckoutOrder")
        .WithTags("Orders")
        .Produces<ApiResponse<CheckoutResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict)
        .Produces<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity);
    }
}
