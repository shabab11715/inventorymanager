namespace InventoryManager.WebApi.Contracts.Inventories;

public record UpdateInventoryRequest(
    string Title,
    string Description,
    string ImageUrl,
    Guid? CategoryId,
    List<string>? Tags,
    uint Version
);