namespace InventoryManager.WebApi.Contracts.Items;

public record UpdateItemRequest(
    string CustomId,
    string Name,
    List<ItemFieldValueInput>? CustomValues,
    uint Version
);