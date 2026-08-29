using MediatR;

namespace OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;

public record CreateShipmentCommand(
    Guid OrderId,
    string ShippingAddress,
    string Carrier = "FedEx") : IRequest<CreateShipmentResponse>;
