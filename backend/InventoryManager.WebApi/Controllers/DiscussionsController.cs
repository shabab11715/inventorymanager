using InventoryManager.Application.Interfaces;
using InventoryManager.Infrastructure.Persistence;
using InventoryManager.WebApi.Auth;
using InventoryManager.WebApi.Contracts.Discussions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.WebApi.Controllers;

[ApiController]
[Route("api/inventories/{inventoryId:guid}/discussions")]
public class DiscussionsController : ControllerBase
{
    private readonly IDiscussionRepository _discussions;
    private readonly IInventoryAccessService _inventoryAccessService;
    private readonly InventoryManagerDbContext _db;

    public DiscussionsController(
        IDiscussionRepository discussions,
        IInventoryAccessService inventoryAccessService,
        InventoryManagerDbContext db)
    {
        _discussions = discussions;
        _inventoryAccessService = inventoryAccessService;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<DiscussionResponse>>> GetByInventory(
        Guid inventoryId,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var userId = User.Identity?.IsAuthenticated == true ? UserContext.GetUserId(User) : Guid.Empty;
        var isAdmin = User.Identity?.IsAuthenticated == true && RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanReadAsync(inventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        var list = await _discussions.GetByInventoryPagedAsync(inventoryId, pageNumber, pageSize);

        var userIds = list.Select(x => x.UserId).Distinct().ToList();

        var userMap = await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var response = list.Select(x =>
            new DiscussionResponse(
                x.Id,
                x.InventoryId,
                x.UserId,
                userMap.TryGetValue(x.UserId, out var userName) ? userName : "Unknown user",
                x.Content,
                x.CreatedAt
            )).ToList();

        return Ok(response);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<DiscussionResponse>> Create(Guid inventoryId, CreateDiscussionRequest request)
    {
        var userId = UserContext.GetUserId(User);
        if (userId == Guid.Empty) return Unauthorized();

        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanReadAsync(inventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "content is required" });
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
        {
            return Unauthorized();
        }

        var discussion = new Domain.Entities.Discussion
        {
            Id = Guid.NewGuid(),
            InventoryId = inventoryId,
            UserId = userId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _discussions.AddAsync(discussion);

        return Ok(new DiscussionResponse(
            discussion.Id,
            discussion.InventoryId,
            discussion.UserId,
            user.Name,
            discussion.Content,
            discussion.CreatedAt
        ));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid inventoryId, Guid id)
    {
        var discussion = await _discussions.GetByIdAsync(id);
        if (discussion is null)
        {
            return NotFound();
        }

        if (discussion.InventoryId != inventoryId)
        {
            return NotFound();
        }

        await _discussions.DeleteAsync(discussion);
        return Ok(new { message = "Deleted" });
    }
}