using InventoryManager.Domain.Common;

namespace InventoryManager.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}