namespace InventoryManager.WebApi.Contracts.Fields;

public record ItemFieldDefinitionInput(
    string FieldType,
    string Title,
    string Description,
    bool ShowInTable
);