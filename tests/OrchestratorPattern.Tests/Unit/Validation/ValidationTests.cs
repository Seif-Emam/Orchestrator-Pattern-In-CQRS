using FluentAssertions;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout;
using OrchestratorPattern.Api.Features.Orders.Commands.CreateOrder;
using OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;
using OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;

namespace OrchestratorPattern.Tests.Unit.Validation;

public class ValidationTests
{
    [Fact]
    public void CreateOrderValidator_WithEmptyItems_ShouldFailValidation()
    {
        var validator = new CreateOrderValidator();
        var command = new CreateOrderCommand(Guid.NewGuid(), new List<CreateOrderItemDto>());

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void CreateOrderValidator_WithNegativeQuantity_ShouldFailValidation()
    {
        var validator = new CreateOrderValidator();
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            new List<CreateOrderItemDto> { new(Guid.NewGuid(), -1) }
        );

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("greater than zero"));
    }

    [Fact]
    public void CheckoutValidator_WithEmptyAddress_ShouldFailValidation()
    {
        var validator = new CheckoutValidator();
        var command = new CheckoutCommand(
            Guid.NewGuid(),
            PaymentMethod.CreditCard,
            "" // Empty address
        );

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShippingAddress");
    }

    [Fact]
    public void ProcessPaymentValidator_WithZeroAmount_ShouldFailValidation()
    {
        var validator = new ProcessPaymentValidator();
        var command = new ProcessPaymentCommand(
            Guid.NewGuid(),
            0m,
            PaymentMethod.CreditCard
        );

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void CreateShipmentValidator_WithShortAddress_ShouldFailValidation()
    {
        var validator = new CreateShipmentValidator();
        var command = new CreateShipmentCommand(
            Guid.NewGuid(),
            "123" // Too short (< 5 chars)
        );

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShippingAddress");
    }
}
