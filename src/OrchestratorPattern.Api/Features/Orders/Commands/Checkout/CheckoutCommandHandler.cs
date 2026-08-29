using MediatR;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout;

/// <summary>
/// Thin Command Handler that delegates checkout workflow execution to the Checkout Orchestrator.
/// </summary>
public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, CheckoutResponse>
{
    private readonly ICheckoutOrchestrator _orchestrator;

    public CheckoutCommandHandler(ICheckoutOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public Task<CheckoutResponse> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        return _orchestrator.CheckoutAsync(request, cancellationToken);
    }
}
