using FluentValidation;

namespace OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;

public class CreateShipmentValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID must not be empty.");

        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage("Shipping address is required.")
            .MinimumLength(5).WithMessage("Shipping address must be at least 5 characters long.")
            .MaximumLength(500).WithMessage("Shipping address must not exceed 500 characters.");

        RuleFor(x => x.Carrier)
            .NotEmpty().WithMessage("Carrier must be specified.")
            .MaximumLength(100).WithMessage("Carrier name must not exceed 100 characters.");
    }
}
