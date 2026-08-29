using MediatR;
using Microsoft.EntityFrameworkCore;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Entities;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Common.Persistence;

namespace OrchestratorPattern.Api.Features.Shipping.Commands.CreateShipment;

public class CreateShipmentHandler : IRequestHandler<CreateShipmentCommand, CreateShipmentResponse>
{
    private readonly AppDbContext _context;
    private readonly ILogger<CreateShipmentHandler> _logger;

    public CreateShipmentHandler(AppDbContext context, ILogger<CreateShipmentHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreateShipmentResponse> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order with ID '{request.OrderId}' was not found.", ErrorCodes.OrderNotFound);
        }

        if (order.Shipment is not null && order.Shipment.Status != ShipmentStatus.Cancelled)
        {
            throw new ConflictException($"Shipment for order '{request.OrderId}' already exists with tracking number '{order.Shipment.TrackingNumber}'.", ErrorCodes.ResourceConflict);
        }

        // Realistic carrier address validation simulation:
        // Addresses containing "INVALID" simulate logistics validation failure
        if (request.ShippingAddress.Contains("INVALID_ADDRESS", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Shipment creation failed for order {OrderId}. Invalid address: {Address}", order.Id, request.ShippingAddress);
            throw new DomainException(
                $"Shipping carrier rejected address '{request.ShippingAddress}'. Please provide a valid street address.",
                ErrorCodes.InvalidShippingAddress);
        }

        var trackingPrefix = request.Carrier.ToUpperInvariant() switch
        {
            "DHL" => "DHL",
            "UPS" => "UPS",
            _ => "FDX"
        };

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Carrier = request.Carrier,
            ShippingAddress = request.ShippingAddress,
            TrackingNumber = $"{trackingPrefix}-{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            Status = ShipmentStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Shipments.AddAsync(shipment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shipment created for Order {OrderId}. Tracking: {TrackingNumber}, Carrier: {Carrier}",
            order.Id, shipment.TrackingNumber, shipment.Carrier);

        return new CreateShipmentResponse(
            shipment.Id,
            order.Id,
            shipment.TrackingNumber,
            shipment.Carrier,
            shipment.ShippingAddress,
            shipment.Status,
            shipment.CreatedAt);
    }
}
