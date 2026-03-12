namespace InventoryManager.WebApi.Contracts.Inventories;

public record UpdateVisibilityRequest(bool IsPublic, uint Version);