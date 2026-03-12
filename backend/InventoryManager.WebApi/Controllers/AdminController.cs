using InventoryManager.Infrastructure.Persistence;
using InventoryManager.WebApi.Auth;
using InventoryManager.WebApi.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryManager.WebApi.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly InventoryManagerDbContext _db;
    private readonly AdminBootstrapOptions _bootstrapOptions;

    public AdminController(
        InventoryManagerDbContext db,
        IOptions<AdminBootstrapOptions> bootstrapOptions)
    {
        _db = db;
        _bootstrapOptions = bootstrapOptions.Value;
    }

    [AllowAnonymous]
    [HttpPost("bootstrap")]
    public async Task<IActionResult> BootstrapAdmin(BootstrapAdminRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "email is required" });
        }

        if (string.IsNullOrWhiteSpace(request.SetupKey))
        {
            return BadRequest(new { message = "setupKey is required" });
        }

        if (string.IsNullOrWhiteSpace(_bootstrapOptions.SetupKey))
        {
            return StatusCode(500, new { message = "Admin bootstrap key is not configured" });
        }

        var adminExists = await _db.Users.AnyAsync(x => x.Role == "admin");
        if (adminExists)
        {
            return BadRequest(new { message = "Bootstrap is disabled because an admin already exists" });
        }

        if (request.SetupKey != _bootstrapOptions.SetupKey)
        {
            return Unauthorized(new { message = "Invalid setup key" });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        if (user is null)
        {
            return NotFound(new { message = "User not found. Use dev-login first to create the user" });
        }

        user.Role = "admin";
        await _db.SaveChangesAsync();

        return Ok(new { message = "Bootstrap admin created" });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserResponse>>> GetUsers()
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Email)
            .Select(x => new AdminUserResponse(
                x.Id,
                x.Email,
                x.Name,
                x.Role,
                x.IsBlocked,
                x.CreatedAt
            ))
            .ToListAsync();

        return Ok(users);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("users/{id:guid}/block")]
    public async Task<IActionResult> BlockUser(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        user.IsBlocked = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "User blocked" });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("users/{id:guid}/unblock")]
    public async Task<IActionResult> UnblockUser(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        user.IsBlocked = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "User unblocked" });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("users/{id:guid}/make-admin")]
    public async Task<IActionResult> MakeAdminById(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        user.Role = "admin";
        await _db.SaveChangesAsync();

        return Ok(new { message = "User is now admin" });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("users/{id:guid}/remove-admin")]
    public async Task<IActionResult> RemoveAdmin(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        user.Role = "user";
        await _db.SaveChangesAsync();

        return Ok(new { message = "Admin role removed" });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        var ownedInventories = await _db.Inventories
            .Where(x => x.OwnerUserId == id)
            .ToListAsync();

        foreach (var inventory in ownedInventories)
        {
            inventory.OwnerUserId = null;
        }

        var writeAccesses = await _db.InventoryWriteAccesses
            .Where(x => x.UserId == id)
            .ToListAsync();

        if (writeAccesses.Count > 0)
        {
            _db.InventoryWriteAccesses.RemoveRange(writeAccesses);
        }

        var likes = await _db.ItemLikes
            .Where(x => x.UserId == id)
            .ToListAsync();

        if (likes.Count > 0)
        {
            _db.ItemLikes.RemoveRange(likes);
        }

        var discussions = await _db.Discussions
            .Where(x => x.UserId == id)
            .ToListAsync();

        if (discussions.Count > 0)
        {
            _db.Discussions.RemoveRange(discussions);
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "User deleted" });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("make-admin")]
    public async Task<IActionResult> MakeAdmin([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "email is required" });
        }

        var normalized = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalized);
        if (user is null)
        {
            return NotFound();
        }

        user.Role = "admin";
        await _db.SaveChangesAsync();

        return Ok(new { message = "User is now admin" });
    }
}