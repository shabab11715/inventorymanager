namespace InventoryManager.WebApi.Contracts.Inventories;

public record AutoSaveInventoryRequest(
    string Title,
    string Description,
    string ImageUrl,
    Guid? CategoryId,
    List<string>? Tags,
    uint Version
);