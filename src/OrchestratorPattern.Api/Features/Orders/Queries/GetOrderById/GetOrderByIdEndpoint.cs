using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Common.Models;
using OrchestratorPattern.Api.Common.Persistence;

namespace OrchestratorPattern.Api.Features.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<GetOrderByIdResponse>;

public record OrderItemDetailDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);

public record OrderPaymentDetailDto(
    Guid PaymentId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? TransactionId,
    string? FailureReason,
    DateTime CreatedAt);

public record OrderShipmentDetailDto(
    Guid ShipmentId,
    string TrackingNumber,
    string Carrier,
    string ShippingAddress,
    ShipmentStatus Status,
    DateTime CreatedAt);

public record GetOrderByIdResponse(
    Guid OrderId,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    OrderStatus Status,
    decimal TotalAmount,
    string? CancellationReason,
    IReadOnlyList<OrderItemDetailDto> Items,
    OrderPaymentDetailDto? Payment,
    OrderShipmentDetailDto? Shipment,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, GetOrderByIdResponse>
{
    private readonly AppDbContext _context;

    public GetOrderByIdHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GetOrderByIdResponse> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order with ID '{request.OrderId}' was not found.", ErrorCodes.OrderNotFound);
        }

        var items = order.Items.Select(i => new OrderItemDetailDto(
            i.ProductId,
            i.Product.Name,
            i.Product.Sku,
            i.UnitPrice,
            i.Quantity,
            i.TotalPrice
        )).ToList();

        var payment = order.Payment is null ? null : new OrderPaymentDetailDto(
            order.Payment.Id,
            order.Payment.Amount,
            order.Payment.PaymentMethod,
            order.Payment.Status,
            order.Payment.TransactionId,
            order.Payment.FailureReason,
            order.Payment.CreatedAt
        );

        var shipment = order.Shipment is null ? null : new OrderShipmentDetailDto(
            order.Shipment.Id,
            order.Shipment.TrackingNumber,
            order.Shipment.Carrier,
            order.Shipment.ShippingAddress,
            order.Shipment.Status,
            order.Shipment.CreatedAt
        );

        return new GetOrderByIdResponse(
            order.Id,
            order.CustomerId,
            order.Customer.FullName,
            order.Customer.Email,
            order.Status,
            order.TotalAmount,
            order.CancellationReason,
            items,
            payment,
            shipment,
            order.CreatedAt,
            order.UpdatedAt
        );
    }
}

public static class GetOrderByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/{id:guid}", async (
            [FromRoute] Guid id,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            return Results.Ok(ApiResponse<GetOrderByIdResponse>.Ok(result, traceId));
        })
        .WithName("GetOrderById")
        .WithTags("Orders")
        .Produces<ApiResponse<GetOrderByIdResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }
}
