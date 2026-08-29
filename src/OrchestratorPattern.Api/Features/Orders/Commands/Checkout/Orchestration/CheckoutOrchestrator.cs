using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Persistence;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration;

public class CheckoutOrchestrator : ICheckoutOrchestrator
{
    private readonly IOrderValidationStep _orderValidationStep;
    private readonly IInventoryReservationStep _inventoryReservationStep;
    private readonly IPaymentProcessingStep _paymentProcessingStep;
    private readonly IShipmentCreationStep _shipmentCreationStep;
    private readonly IFinalizeCheckoutStep _finalizeCheckoutStep;
    private readonly AppDbContext _context;
    private readonly ILogger<CheckoutOrchestrator> _logger;

    public CheckoutOrchestrator(
        IOrderValidationStep orderValidationStep,
        IInventoryReservationStep inventoryReservationStep,
        IPaymentProcessingStep paymentProcessingStep,
        IShipmentCreationStep shipmentCreationStep,
        IFinalizeCheckoutStep finalizeCheckoutStep,
        AppDbContext context,
        ILogger<CheckoutOrchestrator> logger)
    {
        _orderValidationStep = orderValidationStep;
        _inventoryReservationStep = inventoryReservationStep;
        _paymentProcessingStep = paymentProcessingStep;
        _shipmentCreationStep = shipmentCreationStep;
        _finalizeCheckoutStep = finalizeCheckoutStep;
        _context = context;
        _logger = logger;
    }

    public async Task<CheckoutResponse> CheckoutAsync(CheckoutCommand command, CancellationToken cancellationToken)
    {
        var context = new CheckoutWorkflowContext(command);

        try
        {
            // -------------------------------------------------------------------------------------
            // Step 1: Validate Order
            // -------------------------------------------------------------------------------------
            _logger.LogInformation("[Orchestrator] Executing Step 1/5: Validate Order {OrderId}...", context.OrderId);
            context.Order = await _orderValidationStep.ExecuteAsync(context.OrderId, cancellationToken);

            // -------------------------------------------------------------------------------------
            // Step 2: Reserve Inventory
            // -------------------------------------------------------------------------------------
            _logger.LogInformation("[Orchestrator] Executing Step 2/5: Reserve Inventory for Order {OrderId}...", context.OrderId);
            context.InventoryReservation = await _inventoryReservationStep.ExecuteAsync(context.Order, cancellationToken);

            // -------------------------------------------------------------------------------------
            // Step 3: Process Payment
            // -------------------------------------------------------------------------------------
            _logger.LogInformation("[Orchestrator] Executing Step 3/5: Process Payment for Order {OrderId}...", context.OrderId);
            context.PaymentResult = await _paymentProcessingStep.ExecuteAsync(
                context.Order,
                context.PaymentMethod,
                context.CardNumber,
                cancellationToken);

            // -------------------------------------------------------------------------------------
            // Step 4: Create Shipment
            // -------------------------------------------------------------------------------------
            _logger.LogInformation("[Orchestrator] Executing Step 4/5: Create Shipment for Order {OrderId}...", context.OrderId);
            context.ShipmentResult = await _shipmentCreationStep.ExecuteAsync(
                context.Order,
                context.ShippingAddress,
                context.Carrier,
                cancellationToken);

            // -------------------------------------------------------------------------------------
            // Step 5: Finalize Checkout
            // -------------------------------------------------------------------------------------
            _logger.LogInformation("[Orchestrator] Executing Step 5/5: Finalize Checkout for Order {OrderId}...", context.OrderId);
            context.Result = await _finalizeCheckoutStep.ExecuteAsync(context, cancellationToken);

            _logger.LogInformation("[Orchestrator] Checkout workflow completed successfully for Order {OrderId}.", context.OrderId);
            return context.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Orchestrator] Checkout workflow failed for Order {OrderId}. Initiating orchestrated compensation...", context.OrderId);
            await CompensateWorkflowAsync(context, ex, cancellationToken);
            throw;
        }
    }

    private async Task CompensateWorkflowAsync(CheckoutWorkflowContext context, Exception reason, CancellationToken cancellationToken)
    {
        // Compensate in reverse order of step execution

        // 1. Compensate Payment (Refund) if payment was processed
        if (context.IsPaymentProcessed && context.PaymentResult != null)
        {
            try
            {
                _logger.LogWarning("[Orchestrator Compensation] Triggering payment refund for Order {OrderId}...", context.OrderId);
                await _paymentProcessingStep.CompensateAsync(context.Order, context.PaymentResult, cancellationToken);
            }
            catch (Exception compEx)
            {
                _logger.LogError(compEx, "[Orchestrator Compensation] Failed to compensate payment for Order {OrderId}.", context.OrderId);
            }
        }

        // 2. Compensate Inventory (Release Stock) if inventory was reserved
        if (context.IsInventoryReserved && context.InventoryReservation != null)
        {
            try
            {
                _logger.LogWarning("[Orchestrator Compensation] Triggering inventory release for Order {OrderId}...", context.OrderId);
                await _inventoryReservationStep.CompensateAsync(context.Order, context.InventoryReservation, cancellationToken);
            }
            catch (Exception compEx)
            {
                _logger.LogError(compEx, "[Orchestrator Compensation] Failed to compensate inventory for Order {OrderId}.", context.OrderId);
            }
        }

        // 3. Mark Order as Failed with Failure Reason
        if (context.Order != null)
        {
            try
            {
                _logger.LogWarning("[Orchestrator Compensation] Marking Order {OrderId} status as Failed...", context.OrderId);
                context.Order.Status = OrderStatus.Failed;
                context.Order.CancellationReason = reason.Message;
                context.Order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception compEx)
            {
                _logger.LogError(compEx, "[Orchestrator Compensation] Failed to update Order {OrderId} status to Failed.", context.OrderId);
            }
        }
    }
}
