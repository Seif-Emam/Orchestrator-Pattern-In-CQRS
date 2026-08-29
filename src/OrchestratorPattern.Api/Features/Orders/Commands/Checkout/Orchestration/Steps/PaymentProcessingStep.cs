using MediatR;
using OrchestratorPattern.Api.Common.Domain.Entities;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Persistence;
using OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;

public interface IPaymentProcessingStep
{
    Task<ProcessPaymentResponse> ExecuteAsync(
        Order order,
        PaymentMethod paymentMethod,
        string? cardNumber,
        CancellationToken cancellationToken);

    Task CompensateAsync(
        Order order,
        ProcessPaymentResponse payment,
        CancellationToken cancellationToken);
}

public class PaymentProcessingStep : IPaymentProcessingStep
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentProcessingStep> _logger;

    public PaymentProcessingStep(
        IMediator mediator,
        AppDbContext context,
        ILogger<PaymentProcessingStep> logger)
    {
        _mediator = mediator;
        _context = context;
        _logger = logger;
    }

    public async Task<ProcessPaymentResponse> ExecuteAsync(
        Order order,
        PaymentMethod paymentMethod,
        string? cardNumber,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing payment of {TotalAmount:C} for Order {OrderId}...", order.TotalAmount, order.Id);

        var paymentCommand = new ProcessPaymentCommand(
            order.Id,
            order.TotalAmount,
            paymentMethod,
            cardNumber
        );

        var response = await _mediator.Send(paymentCommand, cancellationToken);
        _logger.LogInformation("Payment processed successfully for Order {OrderId}. Transaction: {TxnId}",
            order.Id, response.TransactionId);

        return response;
    }

    public async Task CompensateAsync(
        Order order,
        ProcessPaymentResponse payment,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Compensating payment for Order {OrderId}: Marking payment {PaymentId} as Refunded...",
            order.Id, payment.PaymentId);

        var paymentRecord = order.Payment ?? await _context.Payments.FindAsync(new object[] { payment.PaymentId }, cancellationToken);
        if (paymentRecord != null)
        {
            paymentRecord.Status = PaymentStatus.Refunded;
            await _context.SaveChangesAsync(CancellationToken.None);
        }

        _logger.LogInformation("Payment compensation successfully completed for Order {OrderId}.", order.Id);
    }
}
