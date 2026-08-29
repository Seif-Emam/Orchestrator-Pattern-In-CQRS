using MediatR;
using OrchestratorPattern.Api.Common.Domain.Entities;
using OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;

namespace OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;

public interface IShipmentCreationStep
{
    Task<CreateShipmentResponse> ExecuteAsync(
        Order order,
        string shippingAddress,
        string carrier,
        CancellationToken cancellationToken);
}

public class ShipmentCreationStep : IShipmentCreationStep
{
    private readonly IMediator _mediator;
    private readonly ILogger<ShipmentCreationStep> _logger;

    public ShipmentCreationStep(IMediator mediator, ILogger<ShipmentCreationStep> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<CreateShipmentResponse> ExecuteAsync(
        Order order,
        string shippingAddress,
        string carrier,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating shipment for Order {OrderId} with carrier {Carrier}...", order.Id, carrier);

        var shipmentCommand = new CreateShipmentCommand(
            order.Id,
            shippingAddress,
            carrier
        );

        var response = await _mediator.Send(shipmentCommand, cancellationToken);
        _logger.LogInformation("Shipment created successfully for Order {OrderId}. Tracking: {TrackingNumber}",
            order.Id, response.TrackingNumber);

        return response;
    }
}
