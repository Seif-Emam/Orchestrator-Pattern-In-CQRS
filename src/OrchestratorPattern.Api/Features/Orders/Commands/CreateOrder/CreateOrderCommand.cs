using MediatR;

namespace OrchestratorPattern.Api.Features.Orders.Commands.CreateOrder;

public record CreateOrderItemDto(Guid ProductId, int Quantity);

public record CreateOrderCommand(
    Guid CustomerId,
    IReadOnlyList<CreateOrderItemDto> Items) : IRequest<CreateOrderResponse>;
