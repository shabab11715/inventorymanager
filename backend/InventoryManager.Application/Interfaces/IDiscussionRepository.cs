using InventoryManager.Domain.Entities;

namespace InventoryManager.Application.Interfaces;

public interface IDiscussionRepository
{
    Task<List<Discussion>> GetByInventoryPagedAsync(Guid inventoryId, int pageNumber, int pageSize);
    Task<Discussion?> GetByIdAsync(Guid id);
    Task AddAsync(Discussion discussion);
    Task DeleteAsync(Discussion discussion);
}