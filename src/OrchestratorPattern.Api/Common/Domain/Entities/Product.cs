namespace OrchestratorPattern.Api.Common.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool HasSufficientStock(int quantity) => StockQuantity >= quantity;

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity to reserve must be greater than zero.");
        }

        if (StockQuantity < quantity)
        {
            throw new InvalidOperationException($"Insufficient stock for product '{Name}' (SKU: {Sku}). Requested: {quantity}, Available: {StockQuantity}");
        }

        StockQuantity -= quantity;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity to release must be greater than zero.");
        }

        StockQuantity += quantity;
    }
}
