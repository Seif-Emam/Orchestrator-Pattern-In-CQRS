using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Features.Orders.Commands.CreateOrder;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Unit.Orders;

public class CreateOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCustomerAndProducts_ShouldCreateOrderInPendingStatus()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new CreateOrderCommandHandler(context, NullLogger<CreateOrderCommandHandler>.Instance);

        var customerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var command = new CreateOrderCommand(
            customerId,
            new List<CreateOrderItemDto>
            {
                new(productId, 2)
            }
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().NotBeEmpty();
        result.CustomerId.Should().Be(customerId);
        result.Status.Should().Be(OrderStatus.Pending);
        result.TotalAmount.Should().Be(200.00m); // 2 * $100.00
        result.Items.Should().HaveCount(1);
        result.Items[0].Quantity.Should().Be(2);
        result.Items[0].UnitPrice.Should().Be(100.00m);
    }

    [Fact]
    public async Task Handle_WithNonExistentCustomer_ShouldThrowNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new CreateOrderCommandHandler(context, NullLogger<CreateOrderCommandHandler>.Instance);

        var nonExistentCustomerId = Guid.NewGuid();
        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var command = new CreateOrderCommand(
            nonExistentCustomerId,
            new List<CreateOrderItemDto>
            {
                new(productId, 1)
            }
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentCustomerId}*");
    }

    [Fact]
    public async Task Handle_WithNonExistentProduct_ShouldThrowNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var handler = new CreateOrderCommandHandler(context, NullLogger<CreateOrderCommandHandler>.Instance);

        var customerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var nonExistentProductId = Guid.NewGuid();

        var command = new CreateOrderCommand(
            customerId,
            new List<CreateOrderItemDto>
            {
                new(nonExistentProductId, 1)
            }
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentProductId}*");
    }
}
