using InventoryManager.Domain.Common;

namespace InventoryManager.Domain.Entities;

public class Inventory : BaseEntity
{
    public Guid? OwnerUserId { get; set; }
    public bool IsPublic { get; set; }
    public Guid? CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string CustomIdFormat { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
}