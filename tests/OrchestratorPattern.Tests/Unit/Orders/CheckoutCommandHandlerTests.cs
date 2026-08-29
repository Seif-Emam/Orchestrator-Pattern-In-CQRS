using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout;
using OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;
using OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Unit.Orders;

public class CheckoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenAllStepsSucceed_ShouldConfirmOrderAndReturnCheckoutResponse()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var mediatorMock = new Mock<IMediator>();

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var paymentId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        mediatorMock.Setup(m => m.Send(It.IsAny<ReserveInventoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReserveInventoryResponse(true, new List<ReservedItemDetailDto>(), DateTime.UtcNow));

        mediatorMock.Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessPaymentResponse(paymentId, orderId, 200.00m, PaymentMethod.CreditCard, PaymentStatus.Paid, "txn_12345", DateTime.UtcNow));

        mediatorMock.Setup(m => m.Send(It.IsAny<CreateShipmentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateShipmentResponse(shipmentId, orderId, "FDX-1234567890", "FedEx", "123 Main St", ShipmentStatus.Created, DateTime.UtcNow));

        var handler = new CheckoutCommandHandler(context, mediatorMock.Object, NullLogger<CheckoutCommandHandler>.Instance);

        var command = new CheckoutCommand(
            orderId,
            PaymentMethod.CreditCard,
            "123 Main St, Austin, TX 78701",
            "4111111111111111"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.OrderStatus.Should().Be(OrderStatus.Confirmed);
        result.Payment.PaymentId.Should().Be(paymentId);
        result.Payment.Status.Should().Be(PaymentStatus.Paid);
        result.Shipment.ShipmentId.Should().Be(shipmentId);
        result.Shipment.TrackingNumber.Should().Be("FDX-1234567890");

        var updatedOrder = context.Orders.Find(orderId);
        updatedOrder!.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task Handle_WhenInventoryFails_ShouldMarkOrderFailedAndPropagateException()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var mediatorMock = new Mock<IMediator>();

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        mediatorMock.Setup(m => m.Send(It.IsAny<ReserveInventoryCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Insufficient inventory", ErrorCodes.InsufficientInventory));

        var handler = new CheckoutCommandHandler(context, mediatorMock.Object, NullLogger<CheckoutCommandHandler>.Instance);

        var command = new CheckoutCommand(
            orderId,
            PaymentMethod.CreditCard,
            "123 Main St, Austin, TX 78701"
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InsufficientInventory);

        var updatedOrder = context.Orders.Find(orderId);
        updatedOrder!.Status.Should().Be(OrderStatus.Failed);
    }

    [Fact]
    public async Task Handle_WhenPaymentFails_ShouldCompensateAndReleaseInventory()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var mediatorMock = new Mock<IMediator>();

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var initialStock = context.Products.Find(productId)!.StockQuantity;

        mediatorMock.Setup(m => m.Send(It.IsAny<ReserveInventoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReserveInventoryResponse(true, new List<ReservedItemDetailDto>(), DateTime.UtcNow));

        mediatorMock.Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Card declined", ErrorCodes.PaymentDeclined));

        var handler = new CheckoutCommandHandler(context, mediatorMock.Object, NullLogger<CheckoutCommandHandler>.Instance);

        var command = new CheckoutCommand(
            orderId,
            PaymentMethod.CreditCard,
            "123 Main St, Austin, TX 78701",
            "4000000000000000"
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.PaymentDeclined);

        var updatedOrder = context.Orders.Find(orderId);
        updatedOrder!.Status.Should().Be(OrderStatus.Failed);
        updatedOrder.CancellationReason.Should().Contain("Card declined");
    }

    [Fact]
    public async Task Handle_WhenOrderIsNotPending_ShouldThrowDomainException()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var mediatorMock = new Mock<IMediator>();

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var order = context.Orders.Find(orderId)!;
        order.Status = OrderStatus.Confirmed; // Already confirmed
        context.SaveChanges();

        var handler = new CheckoutCommandHandler(context, mediatorMock.Object, NullLogger<CheckoutCommandHandler>.Instance);

        var command = new CheckoutCommand(
            orderId,
            PaymentMethod.CreditCard,
            "123 Main St, Austin, TX 78701"
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.OrderInvalidState);
    }
}
