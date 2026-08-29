using OrchestratorPattern.Api.Common.Domain.Enums;

namespace OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;

public record ProcessPaymentResponse(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    PaymentStatus Status,
    string? TransactionId,
    DateTime ProcessedAt);
