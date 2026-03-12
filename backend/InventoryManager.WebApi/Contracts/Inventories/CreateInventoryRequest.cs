namespace InventoryManager.WebApi.Contracts.Inventories;

public record CreateInventoryRequest(
    string Title,
    string Description,
    string ImageUrl,
    Guid? CategoryId,
    List<string>? Tags
);