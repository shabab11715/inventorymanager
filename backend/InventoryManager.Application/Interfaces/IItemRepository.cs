using InventoryManager.Domain.Entities;

namespace InventoryManager.Application.Interfaces;

public interface IItemRepository
{
    Task<List<Item>> GetByInventoryPagedAsync(Guid inventoryId, int pageNumber, int pageSize);
    Task<Item?> GetByIdAsync(Guid id);
    Task AddAsync(Item item);
    Task UpdateAsync(Item item, uint originalVersion);
    Task DeleteAsync(Item item);
    Task<int> GetNextSequenceNumberAsync(Guid inventoryId);
}