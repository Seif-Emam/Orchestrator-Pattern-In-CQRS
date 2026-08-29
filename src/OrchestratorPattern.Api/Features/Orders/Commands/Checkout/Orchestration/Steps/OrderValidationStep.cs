using Microsoft.EntityFrameworkCore;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Entities;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Common.Persistence;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;

public interface IOrderValidationStep
{
    Task<Order> ExecuteAsync(Guid orderId, CancellationToken cancellationToken);
}

public class OrderValidationStep : IOrderValidationStep
{
    private readonly AppDbContext _context;
    private readonly ILogger<OrderValidationStep> _logger;

    public OrderValidationStep(AppDbContext context, ILogger<OrderValidationStep> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Order> ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating Order {OrderId} for checkout...", orderId);

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order with ID '{orderId}' was not found.", ErrorCodes.OrderNotFound);
        }

        if (order.Status != OrderStatus.Pending)
        {
            throw new DomainException(
                $"Order '{order.Id}' cannot be checked out because its current status is '{order.Status}'. Only pending orders can be checked out.",
                ErrorCodes.OrderInvalidState);
        }

        if (!order.Items.Any())
        {
            throw new DomainException("Cannot checkout an empty order. Please add products before checking out.", ErrorCodes.EmptyOrderItems);
        }

        _logger.LogInformation("Order {OrderId} validated successfully with {ItemCount} items, total {TotalAmount:C}",
            order.Id, order.Items.Count, order.TotalAmount);

        return order;
    }
}
