using FluentAssertions;
using Moq;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration;

namespace OrchestratorPattern.Tests.Unit.Orders;

public class CheckoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDelegateDirectlyToCheckoutOrchestrator()
    {
        // Arrange
        var orchestratorMock = new Mock<ICheckoutOrchestrator>();
        var command = new CheckoutCommand(
            Guid.NewGuid(),
            PaymentMethod.CreditCard,
            "123 Innovation Drive, Austin, TX",
            "4111111111111111",
            "FedEx"
        );

        var expectedResponse = new CheckoutResponse(
            command.OrderId,
            Guid.NewGuid(),
            "Alice Johnson",
            OrderStatus.Confirmed,
            199.99m,
            new CheckoutPaymentSummaryDto(Guid.NewGuid(), 199.99m, PaymentMethod.CreditCard, PaymentStatus.Paid, "txn_123"),
            new CheckoutShipmentSummaryDto(Guid.NewGuid(), "FDX-123", "FedEx", command.ShippingAddress, ShipmentStatus.Created),
            DateTime.UtcNow
        );

        orchestratorMock
            .Setup(o => o.CheckoutAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var handler = new CheckoutCommandHandler(orchestratorMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
        orchestratorMock.Verify(o => o.CheckoutAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }
}
