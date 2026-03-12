using InventoryManager.Domain.Common;

namespace InventoryManager.Domain.Entities;

public class Discussion : BaseEntity
{
    public Guid InventoryId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}