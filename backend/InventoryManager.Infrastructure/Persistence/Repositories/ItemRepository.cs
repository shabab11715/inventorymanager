using InventoryManager.Application.Interfaces;
using InventoryManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Infrastructure.Persistence.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly InventoryManagerDbContext _db;

    public ItemRepository(InventoryManagerDbContext db)
    {
        _db = db;
    }

    public async Task<List<Item>> GetByInventoryPagedAsync(Guid inventoryId, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        return await _db.Items
            .AsNoTracking()
            .Where(x => x.InventoryId == inventoryId)
            .OrderBy(x => x.CustomId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Item?> GetByIdAsync(Guid id)
    {
        return await _db.Items.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(Item item)
    {
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Item item, uint originalVersion)
    {
        var entry = _db.Entry(item);

        if (entry.State == EntityState.Detached)
        {
            _db.Items.Attach(item);
            entry = _db.Entry(item);
        }

        entry.Property(x => x.Version).OriginalValue = originalVersion;
        entry.Property(x => x.CustomId).IsModified = true;
        entry.Property(x => x.Name).IsModified = true;
        
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Item item)
    {
        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetNextSequenceNumberAsync(Guid inventoryId)
    {
        var maxSequence = await _db.Items
            .Where(x => x.InventoryId == inventoryId)
            .MaxAsync(x => (int?)x.SequenceNumber);

        return (maxSequence ?? 0) + 1;
    }
}