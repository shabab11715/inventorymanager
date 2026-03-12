namespace InventoryManager.WebApi.Contracts.Items;

public record ItemResponse(
    Guid Id,
    Guid InventoryId,
    string CustomId,
    string Name,
    uint Version,
    int LikeCount,
    List<ItemFieldValueResponse> CustomValues
);