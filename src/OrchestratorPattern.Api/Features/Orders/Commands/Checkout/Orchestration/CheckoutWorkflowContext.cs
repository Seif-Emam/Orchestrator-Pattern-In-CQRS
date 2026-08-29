using OrchestratorPattern.Api.Common.Domain.Entities;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;
using OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;
using OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration;

/// <summary>
/// Encapsulates the runtime state, inputs, and intermediate results of the checkout workflow.
/// </summary>
public class CheckoutWorkflowContext
{
    public CheckoutCommand Command { get; }
    public Guid OrderId => Command.OrderId;
    public PaymentMethod PaymentMethod => Command.PaymentMethod;
    public string? CardNumber => Command.CardNumber;
    public string ShippingAddress => Command.ShippingAddress;
    public string Carrier => Command.Carrier;

    public Order Order { get; set; } = null!;
    public ReserveInventoryResponse? InventoryReservation { get; set; }
    public ProcessPaymentResponse? PaymentResult { get; set; }
    public CreateShipmentResponse? ShipmentResult { get; set; }
    public CheckoutResponse? Result { get; set; }

    public bool IsInventoryReserved => InventoryReservation != null && InventoryReservation.Success;
    public bool IsPaymentProcessed => PaymentResult != null && PaymentResult.Status == PaymentStatus.Paid;
    public bool IsShipmentCreated => ShipmentResult != null && ShipmentResult.Status == ShipmentStatus.Created;

    public CheckoutWorkflowContext(CheckoutCommand command)
    {
        Command = command;
    }
}
