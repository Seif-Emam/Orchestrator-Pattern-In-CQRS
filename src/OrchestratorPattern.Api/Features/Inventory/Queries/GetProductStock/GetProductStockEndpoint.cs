using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Common.Models;
using OrchestratorPattern.Api.Common.Persistence;

namespace OrchestratorPattern.Api.Features.Inventory.Queries.GetProductStock;

public record GetProductStockQuery(Guid ProductId) : IRequest<GetProductStockResponse>;

public record GetProductStockResponse(
    Guid ProductId,
    string Sku,
    string Name,
    decimal Price,
    int StockQuantity,
    bool InStock);

public class GetProductStockHandler : IRequestHandler<GetProductStockQuery, GetProductStockResponse>
{
    private readonly AppDbContext _context;

    public GetProductStockHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GetProductStockResponse> Handle(GetProductStockQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Product with ID '{request.ProductId}' was not found.", ErrorCodes.ProductNotFound);
        }

        return new GetProductStockResponse(
            product.Id,
            product.Sku,
            product.Name,
            product.Price,
            product.StockQuantity,
            product.StockQuantity > 0);
    }
}

public static class GetProductStockEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inventory/products/{id:guid}", async (
            [FromRoute] Guid id,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetProductStockQuery(id), cancellationToken);
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            return Results.Ok(ApiResponse<GetProductStockResponse>.Ok(result, traceId));
        })
        .WithName("GetProductStock")
        .WithTags("Inventory")
        .Produces<ApiResponse<GetProductStockResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }
}
