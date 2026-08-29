using MediatR;
using Microsoft.EntityFrameworkCore;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Entities;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Common.Persistence;

namespace OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;

public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResponse>
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProcessPaymentHandler> _logger;

    public ProcessPaymentHandler(AppDbContext context, ILogger<ProcessPaymentHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProcessPaymentResponse> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order with ID '{request.OrderId}' was not found.", ErrorCodes.OrderNotFound);
        }

        if (order.Payment is not null && order.Payment.Status == PaymentStatus.Paid)
        {
            throw new ConflictException($"Order '{request.OrderId}' has already been paid.", ErrorCodes.ResourceConflict);
        }

        // Realistic payment gateway simulation:
        // Test cards ending with "9999" or "0000" or containing "DECLINE" will simulate payment failure
        var isDeclined = !string.IsNullOrEmpty(request.CardNumber) &&
                         (request.CardNumber.EndsWith("9999") ||
                          request.CardNumber.EndsWith("0000") ||
                          request.CardNumber.Contains("DECLINE", StringComparison.OrdinalIgnoreCase));

        var payment = order.Payment ?? new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            CreatedAt = DateTime.UtcNow
        };

        payment.Amount = request.Amount;
        payment.PaymentMethod = request.PaymentMethod;
        payment.ProcessedAt = DateTime.UtcNow;

        if (isDeclined)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = "Payment declined by issuing bank: Insufficient funds or invalid card.";
            payment.TransactionId = null;

            if (order.Payment is null)
            {
                await _context.Payments.AddAsync(payment, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Payment for order {OrderId} declined. Reason: {FailureReason}", order.Id, payment.FailureReason);

            throw new DomainException(
                "Payment was declined by payment provider. Please verify your payment details and try again.",
                ErrorCodes.PaymentDeclined);
        }

        payment.Status = PaymentStatus.Paid;
        payment.TransactionId = $"txn_{Guid.NewGuid():N}";
        payment.FailureReason = null;

        if (order.Payment is null)
        {
            await _context.Payments.AddAsync(payment, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment of {Amount:C} processed successfully for Order {OrderId}. Transaction: {TxnId}",
            payment.Amount, order.Id, payment.TransactionId);

        return new ProcessPaymentResponse(
            payment.Id,
            order.Id,
            payment.Amount,
            payment.PaymentMethod,
            payment.Status,
            payment.TransactionId,
            payment.ProcessedAt.Value);
    }
}
