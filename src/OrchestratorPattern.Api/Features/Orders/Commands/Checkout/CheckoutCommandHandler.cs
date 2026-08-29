using MediatR;
using Microsoft.EntityFrameworkCore;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Common.Persistence;
using OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;
using OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;
using OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout;

public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, CheckoutResponse>
{
    private readonly AppDbContext _context;
    private readonly IMediator _mediator;
    private readonly ILogger<CheckoutCommandHandler> _logger;

    public CheckoutCommandHandler(
        AppDbContext context,
        IMediator mediator,
        ILogger<CheckoutCommandHandler> logger)
    {
        _context = context;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<CheckoutResponse> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        // -----------------------------------------------------------------------------------------
        // STEP 1: Validate the Order and Cart
        // -----------------------------------------------------------------------------------------
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order with ID '{request.OrderId}' was not found.", ErrorCodes.OrderNotFound);
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

        // Track state for manual procedural compensation
        var inventoryReserved = false;
        var paymentProcessed = false;
        ReserveInventoryResponse? inventoryResult = null;
        ProcessPaymentResponse? paymentResult = null;
        CreateShipmentResponse? shipmentResult = null;

        try
        {
            // -----------------------------------------------------------------------------------------
            // STEP 2: Reserve Inventory
            // In this baseline architecture without an Orchestrator, the checkout command handler
            // directly dispatches commands to other feature boundaries in a synchronous tight chain.
            // -----------------------------------------------------------------------------------------
            _logger.LogInformation("Checkout [Step 1/4]: Reserving inventory for Order {OrderId}...", order.Id);

            var reserveCommand = new ReserveInventoryCommand(
                order.Items.Select(i => new ReserveInventoryItemDto(i.ProductId, i.Quantity)).ToList()
            );

            inventoryResult = await _mediator.Send(reserveCommand, cancellationToken);
            inventoryReserved = true;

            // -----------------------------------------------------------------------------------------
            // STEP 3: Process Payment
            // If payment fails, this handler must manually execute compensating actions.
            // -----------------------------------------------------------------------------------------
            _logger.LogInformation("Checkout [Step 2/4]: Processing payment of {TotalAmount:C} for Order {OrderId}...",
                order.TotalAmount, order.Id);

            var paymentCommand = new ProcessPaymentCommand(
                order.Id,
                order.TotalAmount,
                request.PaymentMethod,
                request.CardNumber
            );

            paymentResult = await _mediator.Send(paymentCommand, cancellationToken);
            paymentProcessed = true;

            // -----------------------------------------------------------------------------------------
            // STEP 4: Create Shipment
            // -----------------------------------------------------------------------------------------
            _logger.LogInformation("Checkout [Step 3/4]: Creating shipment for Order {OrderId}...", order.Id);

            var shipmentCommand = new CreateShipmentCommand(
                order.Id,
                request.ShippingAddress,
                request.Carrier
            );

            shipmentResult = await _mediator.Send(shipmentCommand, cancellationToken);

            // -----------------------------------------------------------------------------------------
            // STEP 5: Finalize and Confirm Order
            // -----------------------------------------------------------------------------------------
            _logger.LogInformation("Checkout [Step 4/4]: Finalizing Order {OrderId} status to Confirmed...", order.Id);

            order.Status = OrderStatus.Confirmed;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} checkout successfully completed.", order.Id);

            return new CheckoutResponse(
                order.Id,
                order.CustomerId,
                order.Customer.FullName,
                order.Status,
                order.TotalAmount,
                new CheckoutPaymentSummaryDto(
                    paymentResult.PaymentId,
                    paymentResult.Amount,
                    paymentResult.PaymentMethod,
                    paymentResult.Status,
                    paymentResult.TransactionId
                ),
                new CheckoutShipmentSummaryDto(
                    shipmentResult.ShipmentId,
                    shipmentResult.TrackingNumber,
                    shipmentResult.Carrier,
                    shipmentResult.ShippingAddress,
                    shipmentResult.Status
                ),
                DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checkout failed for Order {OrderId}. Initiating manual procedural rollback...", order.Id);

            // Manual Compensation Logic (tightly coupled within the handler)
            order.Status = OrderStatus.Failed;
            order.CancellationReason = ex.Message;
            order.UpdatedAt = DateTime.UtcNow;

            // Roll back inventory if it was reserved prior to failure
            if (inventoryReserved)
            {
                _logger.LogWarning("Releasing previously reserved inventory for Order {OrderId} due to downstream checkout failure...", order.Id);
                foreach (var item in order.Items)
                {
                    if (item.Product != null)
                    {
                        item.Product.ReleaseStock(item.Quantity);
                    }
                }
            }

            // Refund payment if payment succeeded before shipment failed
            if (paymentProcessed && order.Payment != null)
            {
                _logger.LogWarning("Compensating payment for Order {OrderId} (marking as Refunded) due to downstream shipment failure...", order.Id);
                order.Payment.Status = PaymentStatus.Refunded;
            }

            await _context.SaveChangesAsync(CancellationToken.None);

            // Re-throw so standard exception handler produces correct HTTP response
            throw;
        }
    }
}
