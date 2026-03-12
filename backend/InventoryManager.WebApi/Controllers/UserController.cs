using System.Security.Claims;
using InventoryManager.Infrastructure.Persistence;
using InventoryManager.WebApi.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.WebApi.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly InventoryManagerDbContext _db;

    public UsersController(InventoryManagerDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet("me/profile")]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        return await BuildProfileResponse(userId);
    }

    [HttpGet("{id:guid}/profile")]
    public async Task<ActionResult<UserProfileResponse>> GetProfile(Guid id)
    {
        return await BuildProfileResponse(id);
    }

    private async Task<ActionResult<UserProfileResponse>> BuildProfileResponse(Guid userId)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
        {
            return NotFound();
        }

        var itemCounts = await _db.Items
            .AsNoTracking()
            .GroupBy(x => x.InventoryId)
            .Select(group => new InventoryItemCountRow(
                group.Key,
                group.Count()
            ))
            .ToListAsync();

        var itemCountMap = itemCounts.ToDictionary(x => x.InventoryId, x => x.Count);

        var ownedRaw = await _db.Inventories
            .AsNoTracking()
            .Where(x => x.OwnerUserId == userId)
            .GroupJoin(
                _db.Categories.AsNoTracking(),
                inventory => inventory.CategoryId,
                category => category.Id,
                (inventory, categories) => new { inventory, categories }
            )
            .SelectMany(
                x => x.categories.DefaultIfEmpty(),
                (x, category) => new InventoryProfileRow(
                    x.inventory.Id,
                    x.inventory.Title,
                    x.inventory.Description,
                    x.inventory.ImageUrl,
                    category != null ? category.Name : "Other"
                )
            )
            .ToListAsync();

        var ownedRows = ownedRaw
            .OrderBy(x => x.Title)
            .Select(x => new UserInventoryCardResponse(
                x.Id,
                x.Title,
                x.Description,
                x.ImageUrl,
                x.CategoryName,
                itemCountMap.TryGetValue(x.Id, out int count) ? count : 0
            ))
            .ToList();

        var writableInventoryIds = await _db.InventoryWriteAccesses
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.InventoryId)
            .Distinct()
            .ToListAsync();

        List<InventoryProfileRow> writableRaw;

        if (writableInventoryIds.Count == 0)
        {
            writableRaw = new List<InventoryProfileRow>();
        }
        else
        {
            writableRaw = await _db.Inventories
                .AsNoTracking()
                .Where(x => writableInventoryIds.Contains(x.Id))
                .GroupJoin(
                    _db.Categories.AsNoTracking(),
                    inventory => inventory.CategoryId,
                    category => category.Id,
                    (inventory, categories) => new { inventory, categories }
                )
                .SelectMany(
                    x => x.categories.DefaultIfEmpty(),
                    (x, category) => new InventoryProfileRow(
                        x.inventory.Id,
                        x.inventory.Title,
                        x.inventory.Description,
                        x.inventory.ImageUrl,
                        category != null ? category.Name : "Other"
                    )
                )
                .ToListAsync();
        }

        var writableRows = writableRaw
            .OrderBy(x => x.Title)
            .Select(x => new UserInventoryCardResponse(
                x.Id,
                x.Title,
                x.Description,
                x.ImageUrl,
                x.CategoryName,
                itemCountMap.TryGetValue(x.Id, out int count) ? count : 0
            ))
            .ToList();

        return Ok(new UserProfileResponse(
            user.Id,
            user.Name,
            user.Email,
            ownedRows,
            writableRows
        ));
    }

    private sealed record InventoryItemCountRow(Guid InventoryId, int Count);
    private sealed record InventoryProfileRow(Guid Id, string Title, string Description, string ImageUrl, string CategoryName);
}