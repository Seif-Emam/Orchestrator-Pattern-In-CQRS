using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Entities;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;
using OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;
using OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Unit.Orders;

public class CheckoutOrchestratorTests
{
    private readonly Mock<IOrderValidationStep> _validationStepMock = new();
    private readonly Mock<IInventoryReservationStep> _inventoryStepMock = new();
    private readonly Mock<IPaymentProcessingStep> _paymentStepMock = new();
    private readonly Mock<IShipmentCreationStep> _shipmentStepMock = new();
    private readonly Mock<IFinalizeCheckoutStep> _finalizeStepMock = new();

    private CheckoutOrchestrator CreateOrchestrator(Api.Common.Persistence.AppDbContext context)
    {
        return new CheckoutOrchestrator(
            _validationStepMock.Object,
            _inventoryStepMock.Object,
            _paymentStepMock.Object,
            _shipmentStepMock.Object,
            _finalizeStepMock.Object,
            context,
            NullLogger<CheckoutOrchestrator>.Instance
        );
    }

    [Fact]
    public async Task CheckoutAsync_WhenAllStepsSucceed_ShouldExecuteAllStepsInOrderAndReturnResult()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var orchestrator = CreateOrchestrator(context);

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var order = context.Orders.Find(orderId)!;

        var command = new CheckoutCommand(
            orderId,
            PaymentMethod.CreditCard,
            "123 Innovation Drive, Austin, TX 78701",
            "4111111111111111",
            "FedEx"
        );

        var inventoryResponse = new ReserveInventoryResponse(true, new List<ReservedItemDetailDto>(), DateTime.UtcNow);
        var paymentResponse = new ProcessPaymentResponse(Guid.NewGuid(), orderId, 200.00m, PaymentMethod.CreditCard, PaymentStatus.Paid, "txn_123", DateTime.UtcNow);
        var shipmentResponse = new CreateShipmentResponse(Guid.NewGuid(), orderId, "FDX-12345", "FedEx", command.ShippingAddress, ShipmentStatus.Created, DateTime.UtcNow);
        var expectedResult = new CheckoutResponse(
            orderId,
            order.CustomerId,
            "Test Customer",
            OrderStatus.Confirmed,
            200.00m,
            new CheckoutPaymentSummaryDto(paymentResponse.PaymentId, paymentResponse.Amount, paymentResponse.PaymentMethod, paymentResponse.Status, paymentResponse.TransactionId),
            new CheckoutShipmentSummaryDto(shipmentResponse.ShipmentId, shipmentResponse.TrackingNumber, shipmentResponse.Carrier, shipmentResponse.ShippingAddress, shipmentResponse.Status),
            DateTime.UtcNow
        );

        _validationStepMock.Setup(s => s.ExecuteAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _inventoryStepMock.Setup(s => s.ExecuteAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryResponse);

        _paymentStepMock.Setup(s => s.ExecuteAsync(order, command.PaymentMethod, command.CardNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentResponse);

        _shipmentStepMock.Setup(s => s.ExecuteAsync(order, command.ShippingAddress, command.Carrier, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shipmentResponse);

        _finalizeStepMock.Setup(s => s.ExecuteAsync(It.IsAny<CheckoutWorkflowContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await orchestrator.CheckoutAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);

        // Verify each step was invoked once
        _validationStepMock.Verify(s => s.ExecuteAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
        _inventoryStepMock.Verify(s => s.ExecuteAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _paymentStepMock.Verify(s => s.ExecuteAsync(order, command.PaymentMethod, command.CardNumber, It.IsAny<CancellationToken>()), Times.Once);
        _shipmentStepMock.Verify(s => s.ExecuteAsync(order, command.ShippingAddress, command.Carrier, It.IsAny<CancellationToken>()), Times.Once);
        _finalizeStepMock.Verify(s => s.ExecuteAsync(It.IsAny<CheckoutWorkflowContext>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify no compensation was triggered
        _inventoryStepMock.Verify(s => s.CompensateAsync(It.IsAny<Order>(), It.IsAny<ReserveInventoryResponse>(), It.IsAny<CancellationToken>()), Times.Never);
        _paymentStepMock.Verify(s => s.CompensateAsync(It.IsAny<Order>(), It.IsAny<ProcessPaymentResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_WhenValidationFails_ShouldHaltWorkflowImmediately()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var orchestrator = CreateOrchestrator(context);

        var orderId = Guid.NewGuid();
        var command = new CheckoutCommand(orderId, PaymentMethod.CreditCard, "123 Main St");

        _validationStepMock.Setup(s => s.ExecuteAsync(orderId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException($"Order '{orderId}' not found", ErrorCodes.OrderNotFound));

        // Act & Assert
        var act = () => orchestrator.CheckoutAsync(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();

        // Downstream steps must NOT be called
        _inventoryStepMock.Verify(s => s.ExecuteAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _paymentStepMock.Verify(s => s.ExecuteAsync(It.IsAny<Order>(), It.IsAny<PaymentMethod>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _shipmentStepMock.Verify(s => s.ExecuteAsync(It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _finalizeStepMock.Verify(s => s.ExecuteAsync(It.IsAny<CheckoutWorkflowContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_WhenInventoryFails_ShouldHaltWorkflowAndMarkOrderFailed()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var orchestrator = CreateOrchestrator(context);

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var order = context.Orders.Find(orderId)!;
        var command = new CheckoutCommand(orderId, PaymentMethod.CreditCard, "123 Main St");

        _validationStepMock.Setup(s => s.ExecuteAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _inventoryStepMock.Setup(s => s.ExecuteAsync(order, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Insufficient inventory", ErrorCodes.InsufficientInventory));

        // Act & Assert
        var act = () => orchestrator.CheckoutAsync(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>();

        // Payment and Shipment must NOT execute
        _paymentStepMock.Verify(s => s.ExecuteAsync(It.IsAny<Order>(), It.IsAny<PaymentMethod>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _shipmentStepMock.Verify(s => s.ExecuteAsync(It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        // Order should be marked Failed
        var updatedOrder = context.Orders.Find(orderId);
        updatedOrder!.Status.Should().Be(OrderStatus.Failed);
    }

    [Fact]
    public async Task CheckoutAsync_WhenPaymentFails_ShouldCompensateInventoryAndMarkOrderFailed()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var orchestrator = CreateOrchestrator(context);

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var order = context.Orders.Find(orderId)!;
        var command = new CheckoutCommand(orderId, PaymentMethod.CreditCard, "123 Main St", "4000000000000000");

        var inventoryResponse = new ReserveInventoryResponse(true, new List<ReservedItemDetailDto>(), DateTime.UtcNow);

        _validationStepMock.Setup(s => s.ExecuteAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _inventoryStepMock.Setup(s => s.ExecuteAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryResponse);

        _paymentStepMock.Setup(s => s.ExecuteAsync(order, command.PaymentMethod, command.CardNumber, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Card declined", ErrorCodes.PaymentDeclined));

        // Act & Assert
        var act = () => orchestrator.CheckoutAsync(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>();

        // Shipment must NOT execute
        _shipmentStepMock.Verify(s => s.ExecuteAsync(It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        // Inventory compensation MUST be executed
        _inventoryStepMock.Verify(s => s.CompensateAsync(order, inventoryResponse, It.IsAny<CancellationToken>()), Times.Once);

        // Order should be marked Failed
        var updatedOrder = context.Orders.Find(orderId);
        updatedOrder!.Status.Should().Be(OrderStatus.Failed);
        updatedOrder.CancellationReason.Should().Contain("Card declined");
    }

    [Fact]
    public async Task CheckoutAsync_WhenShipmentFails_ShouldCompensatePaymentAndInventory()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var orchestrator = CreateOrchestrator(context);

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var order = context.Orders.Find(orderId)!;
        var command = new CheckoutCommand(orderId, PaymentMethod.CreditCard, "INVALID_ADDRESS", "4111111111111111");

        var inventoryResponse = new ReserveInventoryResponse(true, new List<ReservedItemDetailDto>(), DateTime.UtcNow);
        var paymentResponse = new ProcessPaymentResponse(Guid.NewGuid(), orderId, 200.00m, PaymentMethod.CreditCard, PaymentStatus.Paid, "txn_123", DateTime.UtcNow);

        _validationStepMock.Setup(s => s.ExecuteAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _inventoryStepMock.Setup(s => s.ExecuteAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryResponse);

        _paymentStepMock.Setup(s => s.ExecuteAsync(order, command.PaymentMethod, command.CardNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentResponse);

        _shipmentStepMock.Setup(s => s.ExecuteAsync(order, command.ShippingAddress, command.Carrier, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Invalid address", ErrorCodes.InvalidShippingAddress));

        // Act & Assert
        var act = () => orchestrator.CheckoutAsync(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>();

        // Reverse compensation: Payment refund AND Inventory release MUST be called
        _paymentStepMock.Verify(s => s.CompensateAsync(order, paymentResponse, It.IsAny<CancellationToken>()), Times.Once);
        _inventoryStepMock.Verify(s => s.CompensateAsync(order, inventoryResponse, It.IsAny<CancellationToken>()), Times.Once);

        // Order marked Failed
        var updatedOrder = context.Orders.Find(orderId);
        updatedOrder!.Status.Should().Be(OrderStatus.Failed);
    }
}
