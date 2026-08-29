using MediatR;

namespace OrchestratorPattern.Api.Features.Inventory.Commands.ReserveInventory;

public record ReserveInventoryItemDto(Guid ProductId, int Quantity);

public record ReserveInventoryCommand(IReadOnlyList<ReserveInventoryItemDto> Items) : IRequest<ReserveInventoryResponse>;
