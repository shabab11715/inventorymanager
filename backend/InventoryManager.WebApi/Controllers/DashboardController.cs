using InventoryManager.Infrastructure.Persistence;
using InventoryManager.WebApi.Contracts.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.WebApi.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly InventoryManagerDbContext _db;

    public DashboardController(InventoryManagerDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get()
    {
        var itemCounts = await _db.Items
            .AsNoTracking()
            .GroupBy(x => x.InventoryId)
            .Select(group => new InventoryItemCountRow(
                group.Key,
                group.Count()
            ))
            .ToListAsync();

        var itemCountMap = itemCounts.ToDictionary(x => x.InventoryId, x => x.Count);

        var latestInventoriesRaw = await _db.Inventories
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Title)
            .Take(5)
            .Select(x => new InventoryCardRow(
                x.Id,
                x.Title,
                x.Description,
                x.ImageUrl,
                x.OwnerUserId
            ))
            .ToListAsync();

        var latestOwnerIds = latestInventoriesRaw
            .Where(x => x.OwnerUserId.HasValue)
            .Select(x => x.OwnerUserId!.Value)
            .Distinct()
            .ToList();

        var latestOwnerMap = latestOwnerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users
                .AsNoTracking()
                .Where(x => latestOwnerIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);

        var latestInventories = latestInventoriesRaw
            .Select(x => new DashboardInventoryCardResponse(
                x.Id,
                x.Title,
                x.Description,
                x.ImageUrl,
                x.OwnerUserId.HasValue && latestOwnerMap.TryGetValue(x.OwnerUserId.Value, out string? ownerName) ? ownerName : "Deleted user",
                itemCountMap.TryGetValue(x.Id, out int itemCount) ? itemCount : 0
            ))
            .ToList();

        var topInventoryIds = itemCounts
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.InventoryId)
            .Take(5)
            .Select(x => x.InventoryId)
            .ToList();

        var topInventoriesRaw = topInventoryIds.Count == 0
            ? new List<InventoryCardRow>()
            : await _db.Inventories
                .AsNoTracking()
                .Where(x => topInventoryIds.Contains(x.Id))
                .Select(x => new InventoryCardRow(
                    x.Id,
                    x.Title,
                    x.Description,
                    x.ImageUrl,
                    x.OwnerUserId
                ))
                .ToListAsync();

        var topOwnerIds = topInventoriesRaw
            .Where(x => x.OwnerUserId.HasValue)
            .Select(x => x.OwnerUserId!.Value)
            .Distinct()
            .ToList();

        var topOwnerMap = topOwnerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users
                .AsNoTracking()
                .Where(x => topOwnerIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);

        var topInventories = topInventoriesRaw
            .Select(x => new DashboardInventoryCardResponse(
                x.Id,
                x.Title,
                x.Description,
                x.ImageUrl,
                x.OwnerUserId.HasValue && topOwnerMap.TryGetValue(x.OwnerUserId.Value, out string? ownerName) ? ownerName : "Deleted user",
                itemCountMap.TryGetValue(x.Id, out int itemCount) ? itemCount : 0
            ))
            .OrderByDescending(x => x.ItemCount)
            .ThenBy(x => x.Title)
            .ToList();

        var tagRows = await _db.InventoryTags
            .AsNoTracking()
            .Join(
                _db.Tags.AsNoTracking(),
                inventoryTag => inventoryTag.TagId,
                tag => tag.Id,
                (inventoryTag, tag) => new TagJoinRow(
                    inventoryTag.InventoryId,
                    tag.Name
                )
            )
            .ToListAsync();

        var tagCloud = tagRows
            .GroupBy(x => x.Name)
            .Select(group => new DashboardTagResponse(
                group.Key,
                group.Select(x => x.InventoryId).Distinct().Count()
            ))
            .OrderByDescending(x => x.InventoryCount)
            .ThenBy(x => x.Name)
            .Take(30)
            .ToList();

        return Ok(new DashboardResponse(
            latestInventories,
            topInventories,
            tagCloud
        ));
    }

    private sealed record InventoryItemCountRow(Guid InventoryId, int Count);
    private sealed record InventoryCardRow(Guid Id, string Title, string Description, string ImageUrl, Guid? OwnerUserId);
    private sealed record TagJoinRow(Guid InventoryId, string Name);
}