namespace InventoryManager.WebApi.Contracts.Inventories;

public record InventoryResponse(
    Guid Id,
    string Title,
    string Description,
    string ImageUrl,
    Guid? CategoryId,
    string? CategoryName,
    List<string> Tags,
    string CustomIdFormat,
    uint Version
);