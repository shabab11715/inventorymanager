using InventoryManager.Domain.Common;

namespace InventoryManager.Domain.Entities;

public class Item : BaseEntity
{
    public Guid InventoryId { get; set; }
    public string CustomId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? SequenceNumber { get; set; }
}