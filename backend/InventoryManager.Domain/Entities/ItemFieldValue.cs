using InventoryManager.Domain.Common;

namespace InventoryManager.Domain.Entities;

public class ItemFieldValue : BaseEntity
{
    public Guid ItemId { get; set; }
    public Guid FieldDefinitionId { get; set; }
    public string? StringValue { get; set; }
    public string? TextValue { get; set; }
    public double? NumberValue { get; set; }
    public string? LinkValue { get; set; }
    public bool? BooleanValue { get; set; }
}