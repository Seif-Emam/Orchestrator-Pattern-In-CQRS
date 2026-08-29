using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Unit.Orders;

public class OrderValidationStepTests
{
    [Fact]
    public async Task ExecuteAsync_WithPendingOrderHavingItems_ShouldReturnOrder()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var step = new OrderValidationStep(context, NullLogger<OrderValidationStep>.Instance);
        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var result = await step.ExecuteAsync(orderId, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(orderId);
        result.Status.Should().Be(OrderStatus.Pending);
        result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentOrder_ShouldThrowNotFoundException()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var step = new OrderValidationStep(context, NullLogger<OrderValidationStep>.Instance);
        var nonExistentId = Guid.NewGuid();

        var act = () => step.ExecuteAsync(nonExistentId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonPendingOrder_ShouldThrowDomainException()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var order = context.Orders.Find(orderId)!;
        order.Status = OrderStatus.Paid;
        context.SaveChanges();

        var step = new OrderValidationStep(context, NullLogger<OrderValidationStep>.Instance);

        var act = () => step.ExecuteAsync(orderId, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.OrderInvalidState);
    }
}
