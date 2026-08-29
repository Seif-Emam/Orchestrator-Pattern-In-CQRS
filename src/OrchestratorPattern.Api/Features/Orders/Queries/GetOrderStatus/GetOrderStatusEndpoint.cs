using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Common.Models;
using OrchestratorPattern.Api.Common.Persistence;

namespace OrchestratorPattern.Api.Features.Orders.Queries.GetOrderStatus;

public record GetOrderStatusQuery(Guid OrderId) : IRequest<GetOrderStatusResponse>;

public record GetOrderStatusResponse(
    Guid OrderId,
    OrderStatus Status,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public class GetOrderStatusHandler : IRequestHandler<GetOrderStatusQuery, GetOrderStatusResponse>
{
    private readonly AppDbContext _context;

    public GetOrderStatusHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GetOrderStatusResponse> Handle(GetOrderStatusQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order with ID '{request.OrderId}' was not found.", ErrorCodes.OrderNotFound);
        }

        return new GetOrderStatusResponse(
            order.Id,
            order.Status,
            order.CancellationReason,
            order.CreatedAt,
            order.UpdatedAt);
    }
}

public static class GetOrderStatusEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/{id:guid}/status", async (
            [FromRoute] Guid id,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetOrderStatusQuery(id), cancellationToken);
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            return Results.Ok(ApiResponse<GetOrderStatusResponse>.Ok(result, traceId));
        })
        .WithName("GetOrderStatus")
        .WithTags("Orders")
        .Produces<ApiResponse<GetOrderStatusResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }
}
