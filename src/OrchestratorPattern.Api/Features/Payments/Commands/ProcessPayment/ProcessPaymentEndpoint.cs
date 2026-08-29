using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrchestratorPattern.Api.Common.Models;

namespace OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;

public static class ProcessPaymentEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/payments/process", async (
            [FromBody] ProcessPaymentCommand command,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            return Results.Ok(ApiResponse<ProcessPaymentResponse>.Ok(result, traceId));
        })
        .WithName("ProcessPayment")
        .WithTags("Payments")
        .Produces<ApiResponse<ProcessPaymentResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict)
        .Produces<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity);
    }
}
