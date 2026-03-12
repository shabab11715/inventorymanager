using InventoryManager.Application.Interfaces;
using InventoryManager.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManager.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;

namespace InventoryManager.WebApi.Controllers;

[ApiController]
[Route("api/items/{itemId:guid}/likes")]
public class LikesController : ControllerBase
{
    private readonly IItemLikeRepository _likes;
    private readonly IItemRepository _items;
    private readonly IInventoryAccessService _inventoryAccessService;

    public LikesController(
        IItemLikeRepository likes,
        IItemRepository items,
        IInventoryAccessService inventoryAccessService)
    {
        _likes = likes;
        _items = items;
        _inventoryAccessService = inventoryAccessService;
    }

    [HttpGet("count")]
    public async Task<ActionResult<object>> Count(Guid itemId)
    {
        var item = await _items.GetByIdAsync(itemId);
        if (item is null) return NotFound();

        var userId = User.Identity?.IsAuthenticated == true ? UserContext.GetUserId(User) : Guid.Empty;
        var isAdmin = User.Identity?.IsAuthenticated == true && RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanReadAsync(item.InventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        var count = await _likes.CountAsync(itemId);
        return Ok(new { itemId, count });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Like(Guid itemId)
    {
        var item = await _items.GetByIdAsync(itemId);
        if (item is null) return NotFound();

        var userId = UserContext.GetUserId(User);
        if (userId == Guid.Empty) return Unauthorized();

        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanReadAsync(item.InventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        if (await _likes.ExistsAsync(itemId, userId))
            return Ok(new { message = "Already liked" });

        try
        {
            await _likes.AddAsync(new ItemLike
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                UserId = userId
            });
        }
        catch (DbUpdateException)
        {
            return Ok(new { message = "Already liked" });
        }

        return Ok(new { message = "Liked" });
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Unlike(Guid itemId)
    {
        var item = await _items.GetByIdAsync(itemId);
        if (item is null) return NotFound();

        var userId = UserContext.GetUserId(User);
        if (userId == Guid.Empty) return Unauthorized();

        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanReadAsync(item.InventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        await _likes.RemoveAsync(itemId, userId);
        return Ok(new { message = "Unliked" });
    }

    [Authorize]
    [HttpGet("status")]
    public async Task<ActionResult<object>> Status(Guid itemId)
    {
        var item = await _items.GetByIdAsync(itemId);
        if (item is null) return NotFound();

        var userId = UserContext.GetUserId(User);
        if (userId == Guid.Empty) return Unauthorized();

        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanReadAsync(item.InventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        var count = await _likes.CountAsync(itemId);
        var likedByUser = await _likes.ExistsAsync(itemId, userId);

        return Ok(new { itemId, count, likedByUser });
    }
}