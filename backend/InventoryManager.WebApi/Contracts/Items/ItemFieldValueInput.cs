namespace InventoryManager.WebApi.Contracts.Items;

public record ItemFieldValueInput(
    Guid FieldDefinitionId,
    string? StringValue,
    string? TextValue,
    double? NumberValue,
    string? LinkValue,
    bool? BooleanValue
);