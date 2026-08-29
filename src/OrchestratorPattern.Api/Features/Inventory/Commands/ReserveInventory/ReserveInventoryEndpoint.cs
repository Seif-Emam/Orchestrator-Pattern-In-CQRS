using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrchestratorPattern.Api.Common.Models;

namespace OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;

public static class ReserveInventoryEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/inventory/reserve", async (
            [FromBody] ReserveInventoryCommand command,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            return Results.Ok(ApiResponse<ReserveInventoryResponse>.Ok(result, traceId));
        })
        .WithName("ReserveInventory")
        .WithTags("Inventory")
        .Produces<ApiResponse<ReserveInventoryResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity);
    }
}
