using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestratorPattern.Api.Common.Domain.Entities;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;
using OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Unit.Orders;

public class PaymentProcessingStepTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldSendProcessPaymentCommandViaMediator()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var mediatorMock = new Mock<IMediator>();

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var order = context.Orders.Find(orderId)!;

        var expectedResponse = new ProcessPaymentResponse(
            Guid.NewGuid(),
            orderId,
            order.TotalAmount,
            PaymentMethod.CreditCard,
            PaymentStatus.Paid,
            "txn_test123",
            DateTime.UtcNow
        );

        mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var step = new PaymentProcessingStep(mediatorMock.Object, context, NullLogger<PaymentProcessingStep>.Instance);

        var result = await step.ExecuteAsync(order, PaymentMethod.CreditCard, "4111111111111111", CancellationToken.None);

        result.Should().BeEquivalentTo(expectedResponse);
        mediatorMock.Verify(m => m.Send(It.Is<ProcessPaymentCommand>(c => c.OrderId == orderId && c.Amount == order.TotalAmount), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompensateAsync_ShouldMarkPaymentAsRefunded()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var mediatorMock = new Mock<IMediator>();

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var order = context.Orders.Find(orderId)!;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 200.00m,
            PaymentMethod = PaymentMethod.CreditCard,
            Status = PaymentStatus.Paid,
            TransactionId = "txn_123"
        };
        context.Payments.Add(payment);
        order.Payment = payment;
        context.SaveChanges();

        var step = new PaymentProcessingStep(mediatorMock.Object, context, NullLogger<PaymentProcessingStep>.Instance);
        var paymentResponse = new ProcessPaymentResponse(payment.Id, orderId, payment.Amount, payment.PaymentMethod, payment.Status, payment.TransactionId, DateTime.UtcNow);

        // Act
        await step.CompensateAsync(order, paymentResponse, CancellationToken.None);

        // Assert
        var updatedPayment = context.Payments.Find(payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.Refunded);
    }
}
