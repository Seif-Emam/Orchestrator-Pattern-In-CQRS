using OrchestratorPattern.Api.Common.Domain.Enums;

namespace OrchestratorPattern.Api.Features.Orders.Commands.CreateOrder;

public record OrderItemResponseDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);

public record CreateOrderResponse(
    Guid OrderId,
    Guid CustomerId,
    string CustomerName,
    OrderStatus Status,
    decimal TotalAmount,
    IReadOnlyList<OrderItemResponseDto> Items,
    DateTime CreatedAt);
