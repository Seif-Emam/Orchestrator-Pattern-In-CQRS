namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration;

public interface ICheckoutOrchestrator
{
    Task<CheckoutResponse> CheckoutAsync(CheckoutCommand command, CancellationToken cancellationToken);
}
