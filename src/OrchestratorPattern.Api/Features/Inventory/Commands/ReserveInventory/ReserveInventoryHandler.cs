using MediatR;
using Microsoft.EntityFrameworkCore;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Common.Persistence;

namespace OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;

public class ReserveInventoryHandler : IRequestHandler<ReserveInventoryCommand, ReserveInventoryResponse>
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReserveInventoryHandler> _logger;

    public ReserveInventoryHandler(AppDbContext context, ILogger<ReserveInventoryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ReserveInventoryResponse> Handle(ReserveInventoryCommand request, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // 1. Verify existence of all requested products
        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out _))
            {
                throw new NotFoundException($"Product with ID '{item.ProductId}' was not found in catalog.", ErrorCodes.ProductNotFound);
            }
        }

        // 2. Check stock availability for all products before applying reservations
        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            if (!product.HasSufficientStock(item.Quantity))
            {
                _logger.LogWarning("Inventory reservation failed for product {ProductId} ({ProductName}). Requested: {Requested}, Available: {Available}",
                    product.Id, product.Name, item.Quantity, product.StockQuantity);

                var errorCode = product.StockQuantity == 0 ? ErrorCodes.ProductOutOfStock : ErrorCodes.InsufficientInventory;
                throw new DomainException(
                    $"Insufficient inventory for product '{product.Name}' (SKU: {product.Sku}). Requested: {item.Quantity}, Available: {product.StockQuantity}.",
                    errorCode);
            }
        }

        // 3. Deduct stock
        var reservedItems = new List<ReservedItemDetailDto>();
        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            product.ReserveStock(item.Quantity);

            reservedItems.Add(new ReservedItemDetailDto(
                product.Id,
                product.Name,
                product.Sku,
                item.Quantity,
                product.StockQuantity
            ));
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully reserved inventory for {Count} product line items.", reservedItems.Count);

        return new ReserveInventoryResponse(true, reservedItems, DateTime.UtcNow);
    }
}
