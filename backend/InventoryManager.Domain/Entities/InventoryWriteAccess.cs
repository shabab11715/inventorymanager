using InventoryManager.Domain.Common;

namespace InventoryManager.Domain.Entities;

public class InventoryWriteAccess : BaseEntity
{
    public Guid InventoryId { get; set; }
    public Guid UserId { get; set; }
}