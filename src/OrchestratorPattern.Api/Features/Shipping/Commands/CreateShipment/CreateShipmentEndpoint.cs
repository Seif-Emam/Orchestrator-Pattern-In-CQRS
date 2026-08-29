using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrchestratorPattern.Api.Common.Models;

namespace OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;

public static class CreateShipmentEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/shipments", async (
            [FromBody] CreateShipmentCommand command,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            return Results.Ok(ApiResponse<CreateShipmentResponse>.Ok(result, traceId));
        })
        .WithName("CreateShipment")
        .WithTags("Shipping")
        .Produces<ApiResponse<CreateShipmentResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict)
        .Produces<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity);
    }
}
