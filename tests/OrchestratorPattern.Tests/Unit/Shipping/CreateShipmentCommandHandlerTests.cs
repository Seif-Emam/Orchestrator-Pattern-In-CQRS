using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Unit.Shipping;

public class CreateShipmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidAddress_ShouldCreateShipmentWithTrackingNumber()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new CreateShipmentHandler(context, NullLogger<CreateShipmentHandler>.Instance);

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var command = new CreateShipmentCommand(
            orderId,
            "123 Delivery Road, Seattle, WA 98101",
            "FedEx"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.TrackingNumber.Should().StartWith("FDX-");
        result.Carrier.Should().Be("FedEx");
        result.Status.Should().Be(ShipmentStatus.Created);
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ShouldThrowDomainExceptionWithInvalidShippingAddressCode()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new CreateShipmentHandler(context, NullLogger<CreateShipmentHandler>.Instance);

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var command = new CreateShipmentCommand(
            orderId,
            "INVALID_ADDRESS_SIMULATION",
            "FedEx"
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvalidShippingAddress);
    }

    [Fact]
    public async Task Handle_WithNonExistentOrder_ShouldThrowNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new CreateShipmentHandler(context, NullLogger<CreateShipmentHandler>.Instance);

        var nonExistentOrderId = Guid.NewGuid();
        var command = new CreateShipmentCommand(
            nonExistentOrderId,
            "123 Delivery Road, Seattle, WA 98101",
            "FedEx"
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
