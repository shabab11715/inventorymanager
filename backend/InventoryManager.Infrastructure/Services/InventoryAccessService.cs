using InventoryManager.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Infrastructure.Persistence;

public class InventoryAccessService : IInventoryAccessService
{
    private readonly InventoryManagerDbContext _db;

    public InventoryAccessService(InventoryManagerDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CanReadAsync(Guid inventoryId, Guid userId, bool isAdmin)
    {
        if (isAdmin) return true;

        return await _db.Inventories
            .AsNoTracking()
            .AnyAsync(x => x.Id == inventoryId);
    }

    public async Task<bool> CanManageInventoryAsync(Guid inventoryId, Guid userId, bool isAdmin)
    {
        if (isAdmin) return true;
        if (userId == Guid.Empty) return false;

        return await _db.Inventories
            .AsNoTracking()
            .AnyAsync(x => x.Id == inventoryId && x.OwnerUserId == userId);
    }

    public async Task<bool> CanWriteItemsAsync(Guid inventoryId, Guid userId, bool isAdmin)
    {
        if (isAdmin) return true;
        if (userId == Guid.Empty) return false;

        var inventory = await _db.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == inventoryId);

        if (inventory is null) return false;
        if (inventory.OwnerUserId == userId) return true;
        if (inventory.IsPublic) return true;

        return await _db.InventoryWriteAccesses
            .AsNoTracking()
            .AnyAsync(x => x.InventoryId == inventoryId && x.UserId == userId);
    }
}