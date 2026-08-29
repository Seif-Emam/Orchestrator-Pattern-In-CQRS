using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestratorPattern.Api.Common.Domain.Entities;
using OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Unit.Orders;

public class InventoryReservationStepTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldSendReserveInventoryCommandViaMediator()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var mediatorMock = new Mock<IMediator>();

        var expectedResponse = new ReserveInventoryResponse(true, new List<ReservedItemDetailDto>(), DateTime.UtcNow);
        mediatorMock
            .Setup(m => m.Send(It.IsAny<ReserveInventoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var step = new InventoryReservationStep(mediatorMock.Object, context, NullLogger<InventoryReservationStep>.Instance);
        var order = context.Orders.Find(Guid.Parse("99999999-9999-9999-9999-999999999999"))!;

        var result = await step.ExecuteAsync(order, CancellationToken.None);

        result.Should().BeEquivalentTo(expectedResponse);
        mediatorMock.Verify(m => m.Send(It.Is<ReserveInventoryCommand>(c => c.Items.Count == order.Items.Count), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompensateAsync_ShouldRestoreStockForOrderItems()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var mediatorMock = new Mock<IMediator>();

        var step = new InventoryReservationStep(mediatorMock.Object, context, NullLogger<InventoryReservationStep>.Instance);
        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var order = context.Orders.Find(orderId)!;
        var product = context.Products.Find(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"))!;

        var initialStock = product.StockQuantity;
        var reservationResponse = new ReserveInventoryResponse(true, new List<ReservedItemDetailDto>(), DateTime.UtcNow);

        // Act
        await step.CompensateAsync(order, reservationResponse, CancellationToken.None);

        // Assert - stock should have increased by order item quantity (2)
        var updatedProduct = context.Products.Find(product.Id);
        updatedProduct!.StockQuantity.Should().Be(initialStock + 2);
    }
}
