using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;
using OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Unit.Orders;

public class ShipmentCreationStepTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldSendCreateShipmentCommandViaMediator()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var mediatorMock = new Mock<IMediator>();

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var order = context.Orders.Find(orderId)!;

        var expectedResponse = new CreateShipmentResponse(
            Guid.NewGuid(),
            orderId,
            "FDX-998877",
            "FedEx",
            "123 Tech Way, Austin, TX",
            ShipmentStatus.Created,
            DateTime.UtcNow
        );

        mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateShipmentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var step = new ShipmentCreationStep(mediatorMock.Object, NullLogger<ShipmentCreationStep>.Instance);

        var result = await step.ExecuteAsync(order, "123 Tech Way, Austin, TX", "FedEx", CancellationToken.None);

        result.Should().BeEquivalentTo(expectedResponse);
        mediatorMock.Verify(m => m.Send(It.Is<CreateShipmentCommand>(c => c.OrderId == orderId && c.Carrier == "FedEx"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
