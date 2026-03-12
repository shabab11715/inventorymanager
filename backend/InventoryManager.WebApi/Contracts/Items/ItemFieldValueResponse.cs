namespace InventoryManager.WebApi.Contracts.Items;

public record ItemFieldValueResponse(
    Guid FieldDefinitionId,
    string FieldType,
    string Title,
    string? StringValue,
    string? TextValue,
    double? NumberValue,
    string? LinkValue,
    bool? BooleanValue
);