using InventoryManager.Domain.Entities;

namespace InventoryManager.Application.Interfaces;

public interface IItemLikeRepository
{
    Task<bool> ExistsAsync(Guid itemId, Guid userId);
    Task AddAsync(ItemLike like);
    Task RemoveAsync(Guid itemId, Guid userId);
    Task<int> CountAsync(Guid itemId);
    Task<Dictionary<Guid, int>> GetCountsAsync(List<Guid> itemIds);
}