using InventoryManager.Domain.Common;

namespace InventoryManager.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}