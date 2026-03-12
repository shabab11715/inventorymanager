using InventoryManager.Domain.Common;

namespace InventoryManager.Domain.Entities;

public class ItemFieldDefinition : BaseEntity
{
    public Guid InventoryId { get; set; }
    public string FieldType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool ShowInTable { get; set; }
    public int DisplayOrder { get; set; }
}