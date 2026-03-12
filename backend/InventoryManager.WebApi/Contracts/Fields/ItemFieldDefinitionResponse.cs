namespace InventoryManager.WebApi.Contracts.Fields;

public record ItemFieldDefinitionResponse(
    Guid Id,
    Guid InventoryId,
    string FieldType,
    string Title,
    string Description,
    bool ShowInTable,
    int DisplayOrder
);