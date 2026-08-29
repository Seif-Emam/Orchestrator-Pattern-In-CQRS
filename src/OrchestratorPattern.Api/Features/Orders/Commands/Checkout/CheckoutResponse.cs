using OrchestratorPattern.Api.Common.Domain.Enums;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout;

public record CheckoutPaymentSummaryDto(
    Guid PaymentId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? TransactionId);

public record CheckoutShipmentSummaryDto(
    Guid ShipmentId,
    string TrackingNumber,
    string Carrier,
    string ShippingAddress,
    ShipmentStatus Status);

public record CheckoutResponse(
    Guid OrderId,
    Guid CustomerId,
    string CustomerName,
    OrderStatus OrderStatus,
    decimal TotalAmount,
    CheckoutPaymentSummaryDto Payment,
    CheckoutShipmentSummaryDto Shipment,
    DateTime CompletedAt);
