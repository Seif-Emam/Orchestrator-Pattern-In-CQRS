using MediatR;
using OrchestratorPattern.Api.Common.Domain.Enums;

namespace OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;

public record ProcessPaymentCommand(
    Guid OrderId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string? CardNumber = null) : IRequest<ProcessPaymentResponse>;
