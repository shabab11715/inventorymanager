using InventoryManager.Application.Interfaces;
using InventoryManager.Domain.Entities;
using InventoryManager.Infrastructure.Persistence;
using InventoryManager.WebApi.Auth;
using InventoryManager.WebApi.Contracts.Fields;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.WebApi.Controllers;

[ApiController]
[Route("api/inventories/{inventoryId:guid}/fields")]
public class InventoryFieldsController : ControllerBase
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "string",
        "text",
        "number",
        "link",
        "boolean"
    };

    private readonly InventoryManagerDbContext _db;
    private readonly IInventoryAccessService _inventoryAccessService;

    public InventoryFieldsController(
        InventoryManagerDbContext db,
        IInventoryAccessService inventoryAccessService)
    {
        _db = db;
        _inventoryAccessService = inventoryAccessService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ItemFieldDefinitionResponse>>> Get(Guid inventoryId)
    {
        var userId = User.Identity?.IsAuthenticated == true ? UserContext.GetUserId(User) : Guid.Empty;
        var isAdmin = User.Identity?.IsAuthenticated == true && RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanReadAsync(inventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        var fields = await _db.ItemFieldDefinitions
            .AsNoTracking()
            .Where(x => x.InventoryId == inventoryId)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new ItemFieldDefinitionResponse(
                x.Id,
                x.InventoryId,
                x.FieldType,
                x.Title,
                x.Description,
                x.ShowInTable,
                x.DisplayOrder
            ))
            .ToListAsync();

        return Ok(fields);
    }

    [Authorize]
    [HttpPut]
    public async Task<ActionResult<List<ItemFieldDefinitionResponse>>> Replace(
        Guid inventoryId,
        ReplaceItemFieldDefinitionsRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanManageInventoryAsync(inventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        var inventoryExists = await _db.Inventories.AnyAsync(x => x.Id == inventoryId);
        if (!inventoryExists)
        {
            return NotFound();
        }

        if (request.Fields is null)
        {
            return BadRequest(new { message = "fields is required" });
        }

        var normalizedFields = request.Fields
            .Select(x => new ItemFieldDefinitionInput(
                x.FieldType?.Trim().ToLowerInvariant() ?? string.Empty,
                x.Title?.Trim() ?? string.Empty,
                x.Description?.Trim() ?? string.Empty,
                x.ShowInTable
            ))
            .ToList();

        foreach (var field in normalizedFields)
        {
            if (!AllowedTypes.Contains(field.FieldType))
            {
                return BadRequest(new { message = $"Invalid fieldType: {field.FieldType}" });
            }

            if (string.IsNullOrWhiteSpace(field.Title))
            {
                return BadRequest(new { message = "Each field must have a title" });
            }
        }

        var countsByType = normalizedFields
            .GroupBy(x => x.FieldType)
            .ToDictionary(x => x.Key, x => x.Count());

        foreach (var pair in countsByType)
        {
            if (pair.Value > 3)
            {
                return BadRequest(new { message = $"Field type '{pair.Key}' exceeds the maximum of 3" });
            }
        }

        var existing = await _db.ItemFieldDefinitions
            .Where(x => x.InventoryId == inventoryId)
            .ToListAsync();

        if (existing.Count > 0)
        {
            _db.ItemFieldDefinitions.RemoveRange(existing);
        }

        var newEntities = normalizedFields
            .Select((field, index) => new ItemFieldDefinition
            {
                Id = Guid.NewGuid(),
                InventoryId = inventoryId,
                FieldType = field.FieldType,
                Title = field.Title,
                Description = field.Description,
                ShowInTable = field.ShowInTable,
                DisplayOrder = index + 1
            })
            .ToList();

        if (newEntities.Count > 0)
        {
            _db.ItemFieldDefinitions.AddRange(newEntities);
        }

        await _db.SaveChangesAsync();

        var response = newEntities
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new ItemFieldDefinitionResponse(
                x.Id,
                x.InventoryId,
                x.FieldType,
                x.Title,
                x.Description,
                x.ShowInTable,
                x.DisplayOrder
            ))
            .ToList();

        return Ok(response);
    }
}