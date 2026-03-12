namespace InventoryManager.Application.Interfaces;

public interface IInventoryAccessService
{
    Task<bool> CanReadAsync(Guid inventoryId, Guid userId, bool isAdmin);
    Task<bool> CanManageInventoryAsync(Guid inventoryId, Guid userId, bool isAdmin);
    Task<bool> CanWriteItemsAsync(Guid inventoryId, Guid userId, bool isAdmin);
}