using InventoryManager.Domain.Common;

namespace InventoryManager.Domain.Entities;

public class InventoryTag : BaseEntity
{
    public Guid InventoryId { get; set; }
    public Guid TagId { get; set; }
}