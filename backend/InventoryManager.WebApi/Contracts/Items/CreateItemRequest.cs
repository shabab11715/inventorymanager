namespace InventoryManager.WebApi.Contracts.Items;

public record CreateItemRequest(
    string CustomId,
    string Name,
    List<ItemFieldValueInput>? CustomValues
);