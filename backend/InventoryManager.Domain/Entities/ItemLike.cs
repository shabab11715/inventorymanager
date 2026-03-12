using InventoryManager.Domain.Common;

namespace InventoryManager.Domain.Entities;

public class ItemLike : BaseEntity
{
    public Guid ItemId { get; set; }
    public Guid UserId { get; set; }
}