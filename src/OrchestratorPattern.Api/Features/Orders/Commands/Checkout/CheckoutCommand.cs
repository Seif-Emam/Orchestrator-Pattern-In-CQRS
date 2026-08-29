using MediatR;
using OrchestratorPattern.Api.Common.Domain.Enums;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout;

public record CheckoutCommand(
    Guid OrderId,
    PaymentMethod PaymentMethod,
    string ShippingAddress,
    string? CardNumber = null,
    string Carrier = "FedEx") : IRequest<CheckoutResponse>;
