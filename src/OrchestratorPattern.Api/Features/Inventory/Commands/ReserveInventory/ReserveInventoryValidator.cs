using FluentValidation;

namespace OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;

public class ReserveInventoryValidator : AbstractValidator<ReserveInventoryCommand>
{
    public ReserveInventoryValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item must be specified for inventory reservation.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product ID must not be empty.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity to reserve must be greater than zero.");
        });
    }
}
