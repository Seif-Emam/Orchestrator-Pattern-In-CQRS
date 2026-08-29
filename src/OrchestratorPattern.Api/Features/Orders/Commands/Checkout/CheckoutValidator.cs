using FluentValidation;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout;

public class CheckoutValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("Valid payment method is required.");

        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage("Shipping address is required.")
            .MinimumLength(5).WithMessage("Shipping address must be at least 5 characters long.");

        RuleFor(x => x.Carrier)
            .NotEmpty().WithMessage("Shipping carrier must be specified.");
    }
}
