using InventoryManager.Application.Interfaces;
using InventoryManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Infrastructure.Persistence.Repositories;

public class DiscussionRepository : IDiscussionRepository
{
    private readonly InventoryManagerDbContext _db;

    public DiscussionRepository(InventoryManagerDbContext db)
    {
        _db = db;
    }

    public Task<List<Discussion>> GetByInventoryPagedAsync(Guid inventoryId, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var skip = (pageNumber - 1) * pageSize;

        return _db.Discussions
            .Where(x => x.InventoryId == inventoryId)
            .OrderBy(x => x.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task<Discussion?> GetByIdAsync(Guid id)
    {
        return _db.Discussions.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(Discussion discussion)
    {
        _db.Discussions.Add(discussion);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Discussion discussion)
    {
        _db.Discussions.Remove(discussion);
        await _db.SaveChangesAsync();
    }
}