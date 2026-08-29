namespace OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;

public record ReservedItemDetailDto(Guid ProductId, string ProductName, string Sku, int QuantityReserved, int RemainingStock);

public record ReserveInventoryResponse(
    bool Success,
    IReadOnlyList<ReservedItemDetailDto> ReservedItems,
    DateTime ReservationTimestamp);
