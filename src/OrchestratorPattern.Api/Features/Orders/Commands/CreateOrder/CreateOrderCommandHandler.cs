using MediatR;
using Microsoft.EntityFrameworkCore;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Domain.Entities;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Common.Persistence;

namespace OrchestratorPattern.Api.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly AppDbContext _context;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(AppDbContext context, ILogger<CreateOrderCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify customer exists
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
        {
            throw new NotFoundException($"Customer with ID '{request.CustomerId}' was not found.", ErrorCodes.CustomerNotFound);
        }

        // 2. Fetch products
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out _))
            {
                throw new NotFoundException($"Product with ID '{item.ProductId}' was not found in catalog.", ErrorCodes.ProductNotFound);
            }
        }

        // 3. Construct Order entity
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Customer = customer,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var orderItems = new List<OrderItem>();
        var responseItems = new List<OrderItemResponseDto>();

        foreach (var itemDto in request.Items)
        {
            var product = products[itemDto.ProductId];
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Order = order,
                ProductId = product.Id,
                Product = product,
                UnitPrice = product.Price,
                Quantity = itemDto.Quantity
            };

            orderItems.Add(orderItem);
            responseItems.Add(new OrderItemResponseDto(
                product.Id,
                product.Name,
                product.Sku,
                orderItem.UnitPrice,
                orderItem.Quantity,
                orderItem.TotalPrice
            ));
        }

        order.Items = orderItems;
        order.RecalculateTotal();

        await _context.Orders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created Order {OrderId} for Customer {CustomerId} with Total {TotalAmount:C}",
            order.Id, customer.Id, order.TotalAmount);

        return new CreateOrderResponse(
            order.Id,
            customer.Id,
            customer.FullName,
            order.Status,
            order.TotalAmount,
            responseItems,
            order.CreatedAt);
    }
}
