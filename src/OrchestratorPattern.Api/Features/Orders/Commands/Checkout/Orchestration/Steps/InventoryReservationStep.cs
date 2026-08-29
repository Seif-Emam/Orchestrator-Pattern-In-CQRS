using MediatR;
using OrchestratorPattern.Api.Common.Domain.Entities;
using OrchestratorPattern.Api.Common.Persistence;
using OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;

public interface IInventoryReservationStep
{
    Task<ReserveInventoryResponse> ExecuteAsync(Order order, CancellationToken cancellationToken);
    Task CompensateAsync(Order order, ReserveInventoryResponse reservation, CancellationToken cancellationToken);
}

public class InventoryReservationStep : IInventoryReservationStep
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _context;
    private readonly ILogger<InventoryReservationStep> _logger;

    public InventoryReservationStep(
        IMediator mediator,
        AppDbContext context,
        ILogger<InventoryReservationStep> logger)
    {
        _mediator = mediator;
        _context = context;
        _logger = logger;
    }

    public async Task<ReserveInventoryResponse> ExecuteAsync(Order order, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reserving inventory for {ItemCount} items in Order {OrderId}...", order.Items.Count, order.Id);

        var reserveCommand = new ReserveInventoryCommand(
            order.Items.Select(i => new ReserveInventoryItemDto(i.ProductId, i.Quantity)).ToList()
        );

        var response = await _mediator.Send(reserveCommand, cancellationToken);
        _logger.LogInformation("Inventory successfully reserved for Order {OrderId}.", order.Id);
        return response;
    }

    public async Task CompensateAsync(Order order, ReserveInventoryResponse reservation, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Compensating inventory reservation for Order {OrderId}: Releasing reserved stock...", order.Id);

        foreach (var item in order.Items)
        {
            if (item.Product != null)
            {
                item.Product.ReleaseStock(item.Quantity);
            }
            else
            {
                var product = await _context.Products.FindAsync(new object[] { item.ProductId }, cancellationToken);
                product?.ReleaseStock(item.Quantity);
            }
        }

        await _context.SaveChangesAsync(CancellationToken.None);
        _logger.LogInformation("Inventory reservation successfully compensated for Order {OrderId}.", order.Id);
    }
}
