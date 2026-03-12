namespace InventoryManager.WebApi.Contracts.Fields;

public record ReplaceItemFieldDefinitionsRequest(
    List<ItemFieldDefinitionInput> Fields
);