using Microsoft.EntityFrameworkCore;
using OrchestratorPattern.Api.Common.Domain.Entities;
using OrchestratorPattern.Api.Common.Domain.Enums;
using OrchestratorPattern.Api.Common.Persistence;

namespace OrchestratorPattern.Tests.Common;

public static class TestDbContextFactory
{
    public static AppDbContext CreateInMemoryDbContext(string dbName = "")
    {
        var db = string.IsNullOrEmpty(dbName) ? Guid.NewGuid().ToString() : dbName;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: db)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AppDbContext(options);
        SeedData(context);
        return context;
    }

    private static void SeedData(AppDbContext context)
    {
        var customer = new Customer
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FullName = "Test Customer",
            Email = "test.customer@example.com",
            Address = "100 Test Blvd, Austin, TX 78701",
            CreatedAt = DateTime.UtcNow
        };

        var productInStock = new Product
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Sku = "TEST-PROD-001",
            Name = "Test Product 1",
            Description = "In-stock product",
            Price = 100.00m,
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow
        };

        var productOutOfStock = new Product
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Sku = "TEST-PROD-002",
            Name = "Out of stock Product",
            Description = "Zero stock product",
            Price = 50.00m,
            StockQuantity = 0,
            CreatedAt = DateTime.UtcNow
        };

        var pendingOrder = new Order
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            CustomerId = customer.Id,
            Customer = customer,
            Status = OrderStatus.Pending,
            TotalAmount = 200.00m,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrderId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    ProductId = productInStock.Id,
                    Product = productInStock,
                    UnitPrice = 100.00m,
                    Quantity = 2
                }
            }
        };

        context.Customers.Add(customer);
        context.Products.AddRange(productInStock, productOutOfStock);
        context.Orders.Add(pendingOrder);
        context.SaveChanges();
    }
}
