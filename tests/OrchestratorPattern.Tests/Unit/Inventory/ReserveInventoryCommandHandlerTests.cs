using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Unit.Inventory;

public class ReserveInventoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithAvailableStock_ShouldDecrementStockAndReturnSuccess()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new ReserveInventoryHandler(context, NullLogger<ReserveInventoryHandler>.Instance);

        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var initialStock = context.Products.Find(productId)!.StockQuantity;

        var command = new ReserveInventoryCommand(
            new List<ReserveInventoryItemDto>
            {
                new(productId, 3)
            }
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ReservedItems.Should().HaveCount(1);
        result.ReservedItems[0].QuantityReserved.Should().Be(3);
        result.ReservedItems[0].RemainingStock.Should().Be(initialStock - 3);

        var updatedProduct = context.Products.Find(productId);
        updatedProduct!.StockQuantity.Should().Be(initialStock - 3);
    }

    [Fact]
    public async Task Handle_WithInsufficientStock_ShouldThrowDomainExceptionWithInsufficientInventoryCode()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new ReserveInventoryHandler(context, NullLogger<ReserveInventoryHandler>.Instance);

        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"); // Stock: 10
        var command = new ReserveInventoryCommand(
            new List<ReserveInventoryItemDto>
            {
                new(productId, 999) // Request more than available
            }
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InsufficientInventory);
    }

    [Fact]
    public async Task Handle_WithZeroStock_ShouldThrowDomainExceptionWithProductOutOfStockCode()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new ReserveInventoryHandler(context, NullLogger<ReserveInventoryHandler>.Instance);

        var outOfStockProductId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"); // Stock: 0
        var command = new ReserveInventoryCommand(
            new List<ReserveInventoryItemDto>
            {
                new(outOfStockProductId, 1)
            }
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ProductOutOfStock);
    }

    [Fact]
    public async Task Handle_WithNonExistentProduct_ShouldThrowNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new ReserveInventoryHandler(context, NullLogger<ReserveInventoryHandler>.Instance);

        var nonExistentId = Guid.NewGuid();
        var command = new ReserveInventoryCommand(
            new List<ReserveInventoryItemDto>
            {
                new(nonExistentId, 1)
            }
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ProductNotFound);
    }
}
