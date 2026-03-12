using InventoryManager.Application.Interfaces;
using InventoryManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryManagerDbContext _db;

    public InventoryRepository(InventoryManagerDbContext db)
    {
        _db = db;
    }

    public Task<Inventory?> GetByIdAsync(Guid id)
    {
        return _db.Inventories.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<List<Inventory>> GetPagedAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var skip = (pageNumber - 1) * pageSize;

        return _db.Inventories
            .OrderByDescending(x => x.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task AddAsync(Inventory inventory)
    {
        _db.Inventories.Add(inventory);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Inventory inventory)
    {
        _db.Inventories.Attach(inventory);

        _db.Entry(inventory).Property(x => x.Version).OriginalValue = inventory.Version;

        _db.Entry(inventory).Property(x => x.Title).IsModified = true;
        _db.Entry(inventory).Property(x => x.Description).IsModified = true;
        _db.Entry(inventory).Property(x => x.ImageUrl).IsModified = true;
        _db.Entry(inventory).Property(x => x.CategoryId).IsModified = true;
        _db.Entry(inventory).Property(x => x.IsPublic).IsModified = true;
        _db.Entry(inventory).Property(x => x.CustomIdFormat).IsModified = true;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Inventory inventory)
    {
        _db.Inventories.Remove(inventory);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Inventory>> GetVisiblePagedAsync(Guid userId, bool isAdmin, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var skip = (pageNumber - 1) * pageSize;

        return await _db.Inventories
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }
}