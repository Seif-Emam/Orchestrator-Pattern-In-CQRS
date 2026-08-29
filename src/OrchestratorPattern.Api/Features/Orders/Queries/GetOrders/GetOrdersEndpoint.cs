using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Models;
using OrchestratorPattern.Api.Common.Persistence;

namespace OrchestratorPattern.Api.Features.Orders.Queries.GetOrders;

public record GetOrdersQuery(
    OrderStatus? Status = null,
    int Page = 1,
    int PageSize = 10) : IRequest<GetOrdersResponse>;

public record OrderSummaryDto(
    Guid OrderId,
    Guid CustomerId,
    string CustomerName,
    OrderStatus Status,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAt);

public record GetOrdersResponse(
    IReadOnlyList<OrderSummaryDto> Orders,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, GetOrdersResponse>
{
    private readonly AppDbContext _context;

    public GetOrdersHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GetOrdersResponse> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 10 : (request.PageSize > 50 ? 50 : request.PageSize);

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.CustomerId,
                o.Customer.FullName,
                o.Status,
                o.TotalAmount,
                o.Items.Count,
                o.CreatedAt))
            .ToListAsync(cancellationToken);

        return new GetOrdersResponse(orders, totalCount, page, pageSize, totalPages);
    }
}

public static class GetOrdersEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders", async (
            [FromQuery] OrderStatus? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrdersQuery(status, page <= 0 ? 1 : page, pageSize <= 0 ? 10 : pageSize);
            var result = await sender.Send(query, cancellationToken);
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            return Results.Ok(ApiResponse<GetOrdersResponse>.Ok(result, traceId));
        })
        .WithName("GetOrders")
        .WithTags("Orders")
        .Produces<ApiResponse<GetOrdersResponse>>(StatusCodes.Status200OK);
    }
}
