using InventoryManager.Application.Interfaces;
using InventoryManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Infrastructure.Persistence.Repositories;

public class ItemLikeRepository : IItemLikeRepository
{
    private readonly InventoryManagerDbContext _db;

    public ItemLikeRepository(InventoryManagerDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsAsync(Guid itemId, Guid userId)
    {
        return _db.ItemLikes.AnyAsync(x => x.ItemId == itemId && x.UserId == userId);
    }

    public async Task AddAsync(ItemLike like)
    {
        _db.ItemLikes.Add(like);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid itemId, Guid userId)
    {
        var like = await _db.ItemLikes.FirstOrDefaultAsync(x => x.ItemId == itemId && x.UserId == userId);
        if (like is null) return;

        _db.ItemLikes.Remove(like);
        await _db.SaveChangesAsync();
    }

    public Task<int> CountAsync(Guid itemId)
    {
        return _db.ItemLikes.CountAsync(x => x.ItemId == itemId);
    }

    public async Task<Dictionary<Guid, int>> GetCountsAsync(List<Guid> itemIds)
    {
        if (itemIds.Count == 0) return new Dictionary<Guid, int>();

        var data = await _db.ItemLikes
            .Where(x => itemIds.Contains(x.ItemId))
            .GroupBy(x => x.ItemId)
            .Select(g => new { ItemId = g.Key, Count = g.Count() })
            .ToListAsync();

        return data.ToDictionary(x => x.ItemId, x => x.Count);
    }
}