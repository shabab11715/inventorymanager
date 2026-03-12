using InventoryManager.Domain.Entities;

namespace InventoryManager.Application.Interfaces;

public interface IInventoryRepository
{
    Task<Inventory?> GetByIdAsync(Guid id);
    Task<List<Inventory>> GetPagedAsync(int pageNumber, int pageSize);
    Task AddAsync(Inventory inventory);
    Task UpdateAsync(Inventory inventory);
    Task DeleteAsync(Inventory inventory);
    Task<List<Inventory>> GetVisiblePagedAsync(Guid userId, bool isAdmin, int pageNumber, int pageSize);
}