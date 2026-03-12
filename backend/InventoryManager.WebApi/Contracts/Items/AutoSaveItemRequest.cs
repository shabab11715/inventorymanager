namespace InventoryManager.WebApi.Contracts.Items;

public record AutoSaveItemRequest(
    string CustomId,
    string Name,
    List<ItemFieldValueInput>? CustomValues,
    uint Version
);