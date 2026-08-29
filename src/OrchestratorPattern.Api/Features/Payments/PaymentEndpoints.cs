using OrchestratorPattern.Api.Features.Payments.Commands.ProcessPayment;

namespace OrchestratorPattern.Api.Features.Payments;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        ProcessPaymentEndpoint.Map(app);
        return app;
    }
}
