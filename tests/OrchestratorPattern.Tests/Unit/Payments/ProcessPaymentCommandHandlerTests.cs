using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Unit.Payments;

public class ProcessPaymentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidPayment_ShouldProcessSuccessfullyAndGenerateTransactionId()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new ProcessPaymentHandler(context, NullLogger<ProcessPaymentHandler>.Instance);

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var command = new ProcessPaymentCommand(
            orderId,
            200.00m,
            PaymentMethod.CreditCard,
            "4111111111111111" // Valid card
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.Status.Should().Be(PaymentStatus.Paid);
        result.TransactionId.Should().StartWith("txn_");
        result.Amount.Should().Be(200.00m);
    }

    [Fact]
    public async Task Handle_WithDecliningCard_ShouldThrowDomainExceptionWithPaymentDeclinedCode()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new ProcessPaymentHandler(context, NullLogger<ProcessPaymentHandler>.Instance);

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var command = new ProcessPaymentCommand(
            orderId,
            200.00m,
            PaymentMethod.CreditCard,
            "4000000000000000" // Card ending in 0000 triggers decline
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.PaymentDeclined);

        // Verify payment record in DB is marked Failed
        var payment = context.Payments.FirstOrDefault(p => p.OrderId == orderId);
        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task Handle_WithNonExistentOrder_ShouldThrowNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new ProcessPaymentHandler(context, NullLogger<ProcessPaymentHandler>.Instance);

        var nonExistentOrderId = Guid.NewGuid();
        var command = new ProcessPaymentCommand(
            nonExistentOrderId,
            100.00m,
            PaymentMethod.CreditCard
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
