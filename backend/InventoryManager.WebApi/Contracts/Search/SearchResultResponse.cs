namespace InventoryManager.WebApi.Contracts.Search;

public record SearchResultResponse(
    List<SearchInventoryHit> Inventories,
    List<SearchItemHit> Items
);

public record SearchInventoryHit(
    Guid Id,
    string Title,
    string Description,
    string ImageUrl
);

public record SearchItemHit(
    Guid Id,
    Guid InventoryId,
    string CustomId,
    string Name
);