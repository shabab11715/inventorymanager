namespace InventoryManager.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public uint Version { get; set; }
}