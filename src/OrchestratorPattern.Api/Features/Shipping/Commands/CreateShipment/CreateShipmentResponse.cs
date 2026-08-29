using OrchestratorPattern.Api.Common.Domain.Enums;

namespace OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;

public record CreateShipmentResponse(
    Guid ShipmentId,
    Guid OrderId,
    string TrackingNumber,
    string Carrier,
    string ShippingAddress,
    ShipmentStatus Status,
    DateTime CreatedAt);
