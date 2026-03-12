using System.Text.Json;
using InventoryManager.Application.CustomIds;
using InventoryManager.Application.Interfaces;
using InventoryManager.Domain.Entities;
using InventoryManager.Infrastructure.Persistence;
using InventoryManager.WebApi.Auth;
using InventoryManager.WebApi.Contracts.Inventories;
using InventoryManager.WebApi.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.WebApi.Controllers;

[ApiController]
[Route("api/inventories")]
public class InventoriesController : ControllerBase
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryAccessService _inventoryAccessService;
    private readonly ICustomIdService _customIdService;
    private readonly InventoryManagerDbContext _db;

    public InventoriesController(
        IInventoryRepository inventoryRepository,
        IInventoryAccessService inventoryAccessService,
        ICustomIdService customIdService,
        InventoryManagerDbContext db)
    {
        _inventoryRepository = inventoryRepository;
        _inventoryAccessService = inventoryAccessService;
        _customIdService = customIdService;
        _db = db;
    }

    private async Task<List<string>> GetInventoryTagsAsync(Guid inventoryId)
    {
        return await _db.InventoryTags
            .AsNoTracking()
            .Where(x => x.InventoryId == inventoryId)
            .Join(
                _db.Tags.AsNoTracking(),
                inventoryTag => inventoryTag.TagId,
                tag => tag.Id,
                (inventoryTag, tag) => tag.Name
            )
            .OrderBy(x => x)
            .ToListAsync();
    }

    private async Task ReplaceInventoryTagsAsync(Guid inventoryId, List<string>? tagNames)
    {
        var existingLinks = await _db.InventoryTags
            .Where(x => x.InventoryId == inventoryId)
            .ToListAsync();

        if (existingLinks.Count > 0)
        {
            _db.InventoryTags.RemoveRange(existingLinks);
        }

        var normalizedNames = (tagNames ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .Take(20)
            .ToList();

        if (normalizedNames.Count == 0)
        {
            await _db.SaveChangesAsync();
            return;
        }

        var existingTags = await _db.Tags
            .Where(x => normalizedNames.Contains(x.Name))
            .ToListAsync();

        var existingTagNames = existingTags
            .Select(x => x.Name)
            .ToHashSet();

        var newTags = normalizedNames
            .Where(x => !existingTagNames.Contains(x))
            .Select(x => new Tag
            {
                Id = Guid.NewGuid(),
                Name = x
            })
            .ToList();

        if (newTags.Count > 0)
        {
            _db.Tags.AddRange(newTags);
            existingTags.AddRange(newTags);
        }

        var links = existingTags
            .Select(x => new InventoryTag
            {
                Id = Guid.NewGuid(),
                InventoryId = inventoryId,
                TagId = x.Id
            })
            .ToList();

        if (links.Count > 0)
        {
            _db.InventoryTags.AddRange(links);
        }

        await _db.SaveChangesAsync();
    }

    [HttpGet]
    public async Task<ActionResult<List<InventoryResponse>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var userId = User.Identity?.IsAuthenticated == true ? UserContext.GetUserId(User) : Guid.Empty;
        var isAdmin = User.Identity?.IsAuthenticated == true && RoleContext.IsAdmin(User);

        var inventories = await _inventoryRepository.GetVisiblePagedAsync(userId, isAdmin, pageNumber, pageSize);

        var categoryMap = await _db.Categories
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var inventoryIds = inventories.Select(x => x.Id).ToList();

        var tagPairs = await _db.InventoryTags
            .AsNoTracking()
            .Where(x => inventoryIds.Contains(x.InventoryId))
            .Join(
                _db.Tags.AsNoTracking(),
                inventoryTag => inventoryTag.TagId,
                tag => tag.Id,
                (inventoryTag, tag) => new { inventoryTag.InventoryId, tag.Name }
            )
            .ToListAsync();

        var tagMap = tagPairs
            .GroupBy(x => x.InventoryId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(v => v.Name).OrderBy(v => v).ToList()
            );

        var response = inventories.Select(x =>
            new InventoryResponse(
                x.Id,
                x.Title,
                x.Description,
                x.ImageUrl,
                x.CategoryId,
                x.CategoryId.HasValue && categoryMap.ContainsKey(x.CategoryId.Value) ? categoryMap[x.CategoryId.Value] : null,
                tagMap.ContainsKey(x.Id) ? tagMap[x.Id] : new List<string>(),
                x.CustomIdFormat,
                x.Version
            )).ToList();

        return Ok(response);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryResponse>>> GetCategories()
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CategoryResponse(x.Id, x.Name))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("tag-autocomplete")]
    public async Task<ActionResult<List<TagAutocompleteResponse>>> TagAutocomplete([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Ok(new List<TagAutocompleteResponse>());
        }

        var normalized = term.Trim().ToLowerInvariant();

        var tags = await _db.Tags
            .AsNoTracking()
            .Where(x => x.Name.StartsWith(normalized))
            .OrderBy(x => x.Name)
            .Take(10)
            .Select(x => new TagAutocompleteResponse(x.Id, x.Name))
            .ToListAsync();

        return Ok(tags);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryResponse>> GetById(Guid id)
    {
        var userId = User.Identity?.IsAuthenticated == true ? UserContext.GetUserId(User) : Guid.Empty;
        var isAdmin = User.Identity?.IsAuthenticated == true && RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanReadAsync(id, userId, isAdmin))
        {
            return Forbid();
        }

        var inventory = await _inventoryRepository.GetByIdAsync(id);
        if (inventory is null) return NotFound();

        string? categoryName = null;
        if (inventory.CategoryId.HasValue)
        {
            categoryName = await _db.Categories
                .AsNoTracking()
                .Where(x => x.Id == inventory.CategoryId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
        }

        var tags = await GetInventoryTagsAsync(inventory.Id);

        return Ok(new InventoryResponse(
            inventory.Id,
            inventory.Title,
            inventory.Description,
            inventory.ImageUrl,
            inventory.CategoryId,
            categoryName,
            tags,
            inventory.CustomIdFormat,
            inventory.Version
        ));
    }

    [HttpGet("{id:guid}/permissions")]
    public async Task<ActionResult<object>> GetPermissions(Guid id)
    {
        var userId = User.Identity?.IsAuthenticated == true ? UserContext.GetUserId(User) : Guid.Empty;
        var isAdmin = User.Identity?.IsAuthenticated == true && RoleContext.IsAdmin(User);

        var canRead = await _inventoryAccessService.CanReadAsync(id, userId, isAdmin);
        if (!canRead)
        {
            return Forbid();
        }

        var canManageInventory = await _inventoryAccessService.CanManageInventoryAsync(id, userId, isAdmin);
        var canWriteItems = await _inventoryAccessService.CanWriteItemsAsync(id, userId, isAdmin);

        return Ok(new
        {
            canManageInventory,
            canWriteItems
        });
    }
    
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<InventoryResponse>> Create(CreateInventoryRequest request)
    {
        var userId = UserContext.GetUserId(User);
        if (userId == Guid.Empty) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "title is required" });
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _db.Categories.AnyAsync(x => x.Id == request.CategoryId.Value);
            if (!categoryExists)
            {
                return BadRequest(new { message = "categoryId is invalid" });
            }
        }

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            ImageUrl = request.ImageUrl?.Trim() ?? string.Empty,
            CategoryId = request.CategoryId,
            OwnerUserId = userId,
            IsPublic = false,
            CreatedAt = DateTime.UtcNow
        };

        await _inventoryRepository.AddAsync(inventory);
        await ReplaceInventoryTagsAsync(inventory.Id, request.Tags);

        string? categoryName = null;
        if (inventory.CategoryId.HasValue)
        {
            categoryName = await _db.Categories
                .AsNoTracking()
                .Where(x => x.Id == inventory.CategoryId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
        }

        var tags = await GetInventoryTagsAsync(inventory.Id);

        var response = new InventoryResponse(
            inventory.Id,
            inventory.Title,
            inventory.Description,
            inventory.ImageUrl,
            inventory.CategoryId,
            categoryName,
            tags,
            inventory.CustomIdFormat,
            inventory.Version
        );

        return CreatedAtAction(nameof(GetById), new { id = inventory.Id }, response);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InventoryResponse>> Update(Guid id, UpdateInventoryRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanManageInventoryAsync(id, userId, isAdmin))
        {
            return Forbid();
        }

        var inventory = await _inventoryRepository.GetByIdAsync(id);
        if (inventory is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "title is required" });
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _db.Categories.AnyAsync(x => x.Id == request.CategoryId.Value);
            if (!categoryExists)
            {
                return BadRequest(new { message = "categoryId is invalid" });
            }
        }

        inventory.Title = request.Title.Trim();
        inventory.Description = request.Description?.Trim() ?? string.Empty;
        inventory.ImageUrl = request.ImageUrl?.Trim() ?? string.Empty;
        inventory.CategoryId = request.CategoryId;
        inventory.Version = request.Version;

        try
        {
            await _inventoryRepository.UpdateAsync(inventory);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Version conflict. Reload and try again." });
        }

        await ReplaceInventoryTagsAsync(inventory.Id, request.Tags);

        string? categoryName = null;
        if (inventory.CategoryId.HasValue)
        {
            categoryName = await _db.Categories
                .AsNoTracking()
                .Where(x => x.Id == inventory.CategoryId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
        }

        var tags = await GetInventoryTagsAsync(inventory.Id);

        var response = new InventoryResponse(
            inventory.Id,
            inventory.Title,
            inventory.Description,
            inventory.ImageUrl,
            inventory.CategoryId,
            categoryName,
            tags,
            inventory.CustomIdFormat,
            inventory.Version
        );

        return Ok(response);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanManageInventoryAsync(id, userId, isAdmin))
        {
            return Forbid();
        }

        var inventory = await _inventoryRepository.GetByIdAsync(id);
        if (inventory is null) return NotFound();

        await _inventoryRepository.DeleteAsync(inventory);

        return NoContent();
    }

    [Authorize]
    [HttpPatch("{id:guid}/autosave")]
    public async Task<ActionResult<InventoryResponse>> AutoSave(Guid id, AutoSaveInventoryRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanManageInventoryAsync(id, userId, isAdmin))
        {
            return Forbid();
        }

        var inventory = await _inventoryRepository.GetByIdAsync(id);
        if (inventory is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "title is required" });
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _db.Categories.AnyAsync(x => x.Id == request.CategoryId.Value);
            if (!categoryExists)
            {
                return BadRequest(new { message = "categoryId is invalid" });
            }
        }

        inventory.Title = request.Title.Trim();
        inventory.Description = request.Description?.Trim() ?? string.Empty;
        inventory.ImageUrl = request.ImageUrl?.Trim() ?? string.Empty;
        inventory.CategoryId = request.CategoryId;
        inventory.Version = request.Version;

        try
        {
            await _inventoryRepository.UpdateAsync(inventory);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Version conflict. Reload and try again." });
        }

        await ReplaceInventoryTagsAsync(inventory.Id, request.Tags);

        string? categoryName = null;
        if (inventory.CategoryId.HasValue)
        {
            categoryName = await _db.Categories
                .AsNoTracking()
                .Where(x => x.Id == inventory.CategoryId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
        }

        var tags = await GetInventoryTagsAsync(inventory.Id);

        return Ok(new InventoryResponse(
            inventory.Id,
            inventory.Title,
            inventory.Description,
            inventory.ImageUrl,
            inventory.CategoryId,
            categoryName,
            tags,
            inventory.CustomIdFormat,
            inventory.Version
        ));
    }

    [Authorize]
    [HttpPut("{id:guid}/custom-id-format")]
    public async Task<ActionResult<InventoryResponse>> UpdateCustomIdFormat(Guid id, UpdateCustomIdFormatRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanManageInventoryAsync(id, userId, isAdmin))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.CustomIdFormat))
        {
            return BadRequest(new { message = "customIdFormat is required" });
        }

        var inventory = await _inventoryRepository.GetByIdAsync(id);
        if (inventory is null) return NotFound();

        inventory.CustomIdFormat = request.CustomIdFormat.Trim();
        inventory.Version = request.Version;

        try
        {
            var parsed = JsonSerializer.Deserialize<List<CustomIdFormatElement>>(request.CustomIdFormat);
            if (parsed is null || parsed.Count == 0)
            {
                return BadRequest(new { message = "customIdFormat must contain at least one element" });
            }
        }
        catch
        {
            return BadRequest(new { message = "customIdFormat must be valid JSON" });
        }

        try
        {
            await _inventoryRepository.UpdateAsync(inventory);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Version conflict. Reload and try again." });
        }

        string? categoryName = null;
        if (inventory.CategoryId.HasValue)
        {
            categoryName = await _db.Categories
                .AsNoTracking()
                .Where(x => x.Id == inventory.CategoryId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
        }

        var tags = await GetInventoryTagsAsync(inventory.Id);

        return Ok(new InventoryResponse(
            inventory.Id,
            inventory.Title,
            inventory.Description,
            inventory.ImageUrl,
            inventory.CategoryId,
            categoryName,
            tags,
            inventory.CustomIdFormat,
            inventory.Version
        ));
    }

    [Authorize]
    [HttpGet("{id:guid}/custom-id-preview")]
    public async Task<ActionResult<object>> PreviewCustomId(Guid id, [FromQuery] string? customIdFormat)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanManageInventoryAsync(id, userId, isAdmin))
        {
            return Forbid();
        }

        try
        {
            var generated = string.IsNullOrWhiteSpace(customIdFormat)
                ? await _customIdService.GenerateAsync(id)
                : await _customIdService.GeneratePreviewAsync(id, customIdFormat);

            return Ok(new
            {
                customId = generated.CustomId,
                sequenceNumber = generated.SequenceNumber
            });
        }
        catch (JsonException)
        {
            return BadRequest(new { message = "customIdFormat must be valid JSON" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [Authorize]
    [HttpGet("{id:guid}/visibility-state")]
    public async Task<ActionResult<object>> GetVisibilityState(Guid id)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanManageInventoryAsync(id, userId, isAdmin))
        {
            return Forbid();
        }

        var inventory = await _inventoryRepository.GetByIdAsync(id);
        if (inventory is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            isPublic = inventory.IsPublic,
            version = inventory.Version
        });
    }

    [Authorize]
    [HttpGet("{id:guid}/writers")]
    public async Task<ActionResult<List<WriterResponse>>> GetWriters(Guid id, [FromQuery] string sortBy = "name")
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanManageInventoryAsync(id, userId, isAdmin))
        {
            return Forbid();
        }

        var query = _db.InventoryWriteAccesses
            .AsNoTracking()
            .Where(x => x.InventoryId == id)
            .Join(
                _db.Users.AsNoTracking(),
                access => access.UserId,
                user => user.Id,
                (access, user) => new WriterResponse(user.Id, user.Name, user.Email)
            );

        query = sortBy.Trim().ToLowerInvariant() == "email"
            ? query.OrderBy(x => x.Email)
            : query.OrderBy(x => x.Name);

        var writers = await query.ToListAsync();
        return Ok(writers);
    }

    [Authorize]
    [HttpGet("{id:guid}/writer-autocomplete")]
    public async Task<ActionResult<List<UserAutocompleteResponse>>> WriterAutocomplete(Guid id, [FromQuery] string term)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanManageInventoryAsync(id, userId, isAdmin))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(term))
        {
            return Ok(new List<UserAutocompleteResponse>());
        }

        var normalized = term.Trim().ToLowerInvariant();

        var users = await _db.Users
            .AsNoTracking()
            .Where(u =>
                u.Id != userId &&
                (u.Name.ToLower().StartsWith(normalized) || u.Email.ToLower().StartsWith(normalized)))
            .OrderBy(u => u.Name)
            .Take(10)
            .Select(u => new UserAutocompleteResponse(u.Id, u.Name, u.Email))
            .ToListAsync();

        return Ok(users);
    }

    [Authorize]
    [HttpPost("{id:guid}/writers")]
    public async Task<IActionResult> AddWriter(Guid id, AddWriterRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanManageInventoryAsync(id, userId, isAdmin))
        {
            return Forbid();
        }

        var inventory = await _inventoryRepository.GetByIdAsync(id);
        if (inventory is null) return NotFound();

        if (inventory.IsPublic)
        {
            return BadRequest(new { message = "Public inventories do not need manual writer access." });
        }

        if (inventory.OwnerUserId == request.UserId)
        {
            return BadRequest(new { message = "Owner already has access." });
        }

        var userExists = await _db.Users.AnyAsync(x => x.Id == request.UserId);
        if (!userExists)
        {
            return NotFound(new { message = "User not found." });
        }

        var alreadyExists = await _db.InventoryWriteAccesses
            .AnyAsync(x => x.InventoryId == id && x.UserId == request.UserId);

        if (alreadyExists)
        {
            return Ok(new { message = "User already has write access." });
        }

        _db.InventoryWriteAccesses.Add(new InventoryWriteAccess
        {
            Id = Guid.NewGuid(),
            InventoryId = id,
            UserId = request.UserId
        });

        await _db.SaveChangesAsync();
        return Ok(new { message = "Writer added." });
    }

    [Authorize]
    [HttpDelete("{id:guid}/writers/{writerUserId:guid}")]
    public async Task<IActionResult> RemoveWriter(Guid id, Guid writerUserId)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanManageInventoryAsync(id, userId, isAdmin))
        {
            return Forbid();
        }

        var access = await _db.InventoryWriteAccesses
            .FirstOrDefaultAsync(x => x.InventoryId == id && x.UserId == writerUserId);

        if (access is null)
        {
            return NotFound();
        }

        _db.InventoryWriteAccesses.Remove(access);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}