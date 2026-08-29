using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Persistence;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;

public interface IFinalizeCheckoutStep
{
    Task<CheckoutResponse> ExecuteAsync(CheckoutWorkflowContext context, CancellationToken cancellationToken);
}

public class FinalizeCheckoutStep : IFinalizeCheckoutStep
{
    private readonly AppDbContext _context;
    private readonly ILogger<FinalizeCheckoutStep> _logger;

    public FinalizeCheckoutStep(AppDbContext context, ILogger<FinalizeCheckoutStep> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CheckoutResponse> ExecuteAsync(CheckoutWorkflowContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Finalizing checkout for Order {OrderId}...", context.OrderId);

        context.Order.Status = OrderStatus.Confirmed;
        context.Order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} checkout finalized and marked Confirmed.", context.OrderId);

        var paymentSummary = new CheckoutPaymentSummaryDto(
            context.PaymentResult!.PaymentId,
            context.PaymentResult.Amount,
            context.PaymentResult.PaymentMethod,
            context.PaymentResult.Status,
            context.PaymentResult.TransactionId
        );

        var shipmentSummary = new CheckoutShipmentSummaryDto(
            context.ShipmentResult!.ShipmentId,
            context.ShipmentResult.TrackingNumber,
            context.ShipmentResult.Carrier,
            context.ShipmentResult.ShippingAddress,
            context.ShipmentResult.Status
        );

        return new CheckoutResponse(
            context.Order.Id,
            context.Order.CustomerId,
            context.Order.Customer.FullName,
            context.Order.Status,
            context.Order.TotalAmount,
            paymentSummary,
            shipmentSummary,
            DateTime.UtcNow
        );
    }
}
