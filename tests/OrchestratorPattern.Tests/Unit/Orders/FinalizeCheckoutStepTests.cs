using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;
using OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;
using OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;
using OrchestratorPattern.Tests.Common;

namespace OrchestratorPattern.Tests.Unit.Orders;

public class FinalizeCheckoutStepTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldSetOrderStatusConfirmedAndReturnCheckoutResponse()
    {
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var step = new FinalizeCheckoutStep(context, NullLogger<FinalizeCheckoutStep>.Instance);

        var orderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var order = context.Orders.Find(orderId)!;

        var command = new CheckoutCommand(orderId, PaymentMethod.CreditCard, "123 Tech Way, Austin, TX", "4111111111111111", "FedEx");
        var workflowContext = new CheckoutWorkflowContext(command)
        {
            Order = order,
            PaymentResult = new ProcessPaymentResponse(Guid.NewGuid(), orderId, 200.00m, PaymentMethod.CreditCard, PaymentStatus.Paid, "txn_123", DateTime.UtcNow),
            ShipmentResult = new CreateShipmentResponse(Guid.NewGuid(), orderId, "FDX-12345", "FedEx", command.ShippingAddress, ShipmentStatus.Created, DateTime.UtcNow)
        };

        var result = await step.ExecuteAsync(workflowContext, CancellationToken.None);

        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.OrderStatus.Should().Be(OrderStatus.Confirmed);
        result.Payment.Status.Should().Be(PaymentStatus.Paid);
        result.Shipment.Status.Should().Be(ShipmentStatus.Created);

        var updatedOrder = context.Orders.Find(orderId);
        updatedOrder!.Status.Should().Be(OrderStatus.Confirmed);
    }
}
