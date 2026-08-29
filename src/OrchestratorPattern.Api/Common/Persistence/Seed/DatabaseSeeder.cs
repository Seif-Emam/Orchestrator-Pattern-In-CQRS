using Microsoft.EntityFrameworkCore;
using OrchestratorPattern.Api.Common.Domain.Entities;

namespace OrchestratorPattern.Api.Common.Persistence.Seed;

public static class DatabaseSeeder
{
    public static readonly Guid CustomerAliceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CustomerBobId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid ProductLaptopId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid ProductPhoneId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid ProductHeadphonesId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid ProductOutOfStockId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await context.Customers.AnyAsync(cancellationToken))
        {
            var customers = new List<Customer>
            {
                new()
                {
                    Id = CustomerAliceId,
                    FullName = "Alice Johnson",
                    Email = "alice.johnson@example.com",
                    Address = "123 Technology Way, Silicon Valley, CA 94025",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = CustomerBobId,
                    FullName = "Bob Smith",
                    Email = "bob.smith@example.com",
                    Address = "456 Commerce Blvd, Seattle, WA 98101",
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Customers.AddRangeAsync(customers, cancellationToken);
        }

        if (!await context.Products.AnyAsync(cancellationToken))
        {
            var products = new List<Product>
            {
                new()
                {
                    Id = ProductLaptopId,
                    Sku = "TECH-LAPTOP-001",
                    Name = "Pro Performance Laptop 16\"",
                    Description = "High-performance laptop for software engineering and creative work.",
                    Price = 1499.99m,
                    StockQuantity = 25,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = ProductPhoneId,
                    Sku = "TECH-PHONE-002",
                    Name = "UltraSmart Phone 5G",
                    Description = "Next generation flagship smartphone with high-res camera.",
                    Price = 899.99m,
                    StockQuantity = 50,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = ProductHeadphonesId,
                    Sku = "TECH-AUDIO-003",
                    Name = "Noise-Cancelling Wireless Headphones",
                    Description = "Premium over-ear wireless headphones with active noise cancellation.",
                    Price = 249.99m,
                    StockQuantity = 100,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = ProductOutOfStockId,
                    Sku = "TECH-LIMITED-004",
                    Name = "Limited Edition Mechanical Keyboard",
                    Description = "Custom mechanical keyboard with hot-swappable switches (currently out of stock).",
                    Price = 199.99m,
                    StockQuantity = 0,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Products.AddRangeAsync(products, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
