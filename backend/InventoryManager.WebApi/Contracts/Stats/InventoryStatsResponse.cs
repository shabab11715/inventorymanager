namespace InventoryManager.WebApi.Contracts.Stats;

public record InventoryStatsResponse(
    int ItemCount,
    int TotalLikes,
    List<InventoryNumericFieldStatsResponse> NumericFields,
    List<InventoryTextFieldStatsResponse> TextFields
);

public record InventoryNumericFieldStatsResponse(
    Guid FieldDefinitionId,
    string Title,
    int PopulatedCount,
    double? Min,
    double? Max,
    double? Average
);

public record InventoryTextFieldStatsResponse(
    Guid FieldDefinitionId,
    string Title,
    List<InventoryTopValueResponse> TopValues
);

public record InventoryTopValueResponse(
    string Value,
    int Count
);