using InventoryManager.Application.Interfaces;
using InventoryManager.Domain.Entities;
using InventoryManager.Infrastructure.Persistence;
using InventoryManager.WebApi.Auth;
using InventoryManager.WebApi.Contracts.Items;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.WebApi.Controllers;

[ApiController]
[Route("api/inventories/{inventoryId:guid}/items")]
public class ItemsController : ControllerBase
{
    private readonly IItemRepository _itemRepository;
    private readonly IItemLikeRepository _itemLikeRepository;
    private readonly IInventoryAccessService _inventoryAccessService;
    private readonly ICustomIdService _customIdService;
    private readonly InventoryManagerDbContext _db;

    public ItemsController(
        IItemRepository itemRepository,
        IItemLikeRepository itemLikeRepository,
        IInventoryAccessService inventoryAccessService,
        ICustomIdService customIdService,
        InventoryManagerDbContext db)
    {
        _itemRepository = itemRepository;
        _itemLikeRepository = itemLikeRepository;
        _inventoryAccessService = inventoryAccessService;
        _customIdService = customIdService;
        _db = db;
    }

    private async Task ReplaceItemFieldValuesAsync(Guid inventoryId, Guid itemId, List<ItemFieldValueInput>? values)
    {
        var incomingValues = values ?? new List<ItemFieldValueInput>();
        var fieldIds = incomingValues.Select(x => x.FieldDefinitionId).Distinct().ToList();

        Dictionary<Guid, ItemFieldDefinition> definitions;

        if (fieldIds.Count == 0)
        {
            definitions = new Dictionary<Guid, ItemFieldDefinition>();
        }
        else
        {
            definitions = await _db.ItemFieldDefinitions
                .Where(x => x.InventoryId == inventoryId && fieldIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            if (definitions.Count != fieldIds.Count)
            {
                throw new InvalidOperationException("One or more field definitions are invalid for this inventory.");
            }
        }

        var existing = await _db.ItemFieldValues
            .Where(x => x.ItemId == itemId)
            .ToListAsync();

        if (existing.Count > 0)
        {
            _db.ItemFieldValues.RemoveRange(existing);
        }

        if (incomingValues.Count == 0)
        {
            await _db.SaveChangesAsync();
            return;
        }

        var newValues = new List<ItemFieldValue>();

        foreach (var value in incomingValues)
        {
            var definition = definitions[value.FieldDefinitionId];

            var entity = new ItemFieldValue
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                FieldDefinitionId = definition.Id
            };

            if (definition.FieldType == "string")
            {
                entity.StringValue = value.StringValue;
            }
            else if (definition.FieldType == "text")
            {
                entity.TextValue = value.TextValue;
            }
            else if (definition.FieldType == "number")
            {
                entity.NumberValue = value.NumberValue;
            }
            else if (definition.FieldType == "link")
            {
                entity.LinkValue = value.LinkValue;
            }
            else if (definition.FieldType == "boolean")
            {
                entity.BooleanValue = value.BooleanValue;
            }

            newValues.Add(entity);
        }

        _db.ItemFieldValues.AddRange(newValues);
        await _db.SaveChangesAsync();
    }

    private async Task<List<ItemFieldValueResponse>> GetItemFieldValuesAsync(Guid inventoryId, Guid itemId)
    {
        var definitions = await _db.ItemFieldDefinitions
            .AsNoTracking()
            .Where(x => x.InventoryId == inventoryId)
            .ToDictionaryAsync(x => x.Id);

        var values = await _db.ItemFieldValues
            .AsNoTracking()
            .Where(x => x.ItemId == itemId)
            .ToListAsync();

        return values
            .Where(x => definitions.ContainsKey(x.FieldDefinitionId))
            .Select(x =>
            {
                var definition = definitions[x.FieldDefinitionId];

                return new ItemFieldValueResponse(
                    x.FieldDefinitionId,
                    definition.FieldType,
                    definition.Title,
                    x.StringValue,
                    x.TextValue,
                    x.NumberValue,
                    x.LinkValue,
                    x.BooleanValue
                );
            })
            .ToList();
    }

    [HttpGet]
    public async Task<ActionResult<List<ItemResponse>>> GetPaged(
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

        var items = await _itemRepository.GetByInventoryPagedAsync(inventoryId, pageNumber, pageSize);

        var itemIds = items.Select(x => x.Id).ToList();
        var counts = await _itemLikeRepository.GetCountsAsync(itemIds);

        var customValues = itemIds.Count == 0
            ? new Dictionary<Guid, List<ItemFieldValueResponse>>()
            : await _db.ItemFieldValues
                .AsNoTracking()
                .Where(x => itemIds.Contains(x.ItemId))
                .Join(
                    _db.ItemFieldDefinitions.AsNoTracking(),
                    value => value.FieldDefinitionId,
                    definition => definition.Id,
                    (value, definition) => new
                    {
                        value.ItemId,
                        Response = new ItemFieldValueResponse(
                            value.FieldDefinitionId,
                            definition.FieldType,
                            definition.Title,
                            value.StringValue,
                            value.TextValue,
                            value.NumberValue,
                            value.LinkValue,
                            value.BooleanValue
                        )
                    }
                )
                .GroupBy(x => x.ItemId)
                .ToDictionaryAsync(
                    group => group.Key,
                    group => group.Select(x => x.Response).ToList()
                );

        var response = items.Select(x =>
            new ItemResponse(
                x.Id,
                x.InventoryId,
                x.CustomId,
                x.Name,
                x.Version,
                counts.TryGetValue(x.Id, out var c) ? c : 0,
                customValues.TryGetValue(x.Id, out var values) ? values : new List<ItemFieldValueResponse>()
            )).ToList();

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ItemResponse>> GetById(Guid inventoryId, Guid id)
    {
        var userId = User.Identity?.IsAuthenticated == true ? UserContext.GetUserId(User) : Guid.Empty;
        var isAdmin = User.Identity?.IsAuthenticated == true && RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanReadAsync(inventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        var item = await _itemRepository.GetByIdAsync(id);
        if (item is null) return NotFound();
        if (item.InventoryId != inventoryId) return NotFound();

        var likeCount = await _itemLikeRepository.CountAsync(item.Id);
        var customValues = await GetItemFieldValuesAsync(inventoryId, item.Id);

        return Ok(new ItemResponse(
            item.Id,
            item.InventoryId,
            item.CustomId,
            item.Name,
            item.Version,
            likeCount,
            customValues
        ));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ItemResponse>> Create(Guid inventoryId, CreateItemRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanWriteItemsAsync(inventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "name is required" });
        }

        var requestedCustomId = request.CustomId?.Trim() ?? string.Empty;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var generated = await _customIdService.GenerateAsync(inventoryId);

            var finalCustomId = generated.CustomId;

            if (!string.IsNullOrWhiteSpace(requestedCustomId))
            {
                var isValid = await _customIdService.IsValidAsync(inventoryId, requestedCustomId);
                if (!isValid)
                {
                    return BadRequest(new { message = "customId does not match the inventory format" });
                }

                finalCustomId = requestedCustomId;
            }

            var item = new Item
            {
                Id = Guid.NewGuid(),
                InventoryId = inventoryId,
                CustomId = finalCustomId,
                Name = request.Name.Trim(),
                SequenceNumber = generated.SequenceNumber
            };

            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                await _itemRepository.AddAsync(item);
                await ReplaceItemFieldValuesAsync(inventoryId, item.Id, request.CustomValues);
                await transaction.CommitAsync();

                var customValues = await GetItemFieldValuesAsync(inventoryId, item.Id);
                var response = new ItemResponse(
                    item.Id,
                    item.InventoryId,
                    item.CustomId,
                    item.Name,
                    item.Version,
                    0,
                    customValues
                );

                return CreatedAtAction(nameof(GetById), new { inventoryId = item.InventoryId, id = item.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                if (!string.IsNullOrWhiteSpace(requestedCustomId))
                {
                    return Conflict(new { message = "Duplicate customId in this inventory." });
                }
            }
        }

        return Conflict(new { message = "Generated custom ID conflicted. Please try again." });
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ItemResponse>> Update(Guid inventoryId, Guid id, UpdateItemRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanWriteItemsAsync(inventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        var item = await _itemRepository.GetByIdAsync(id);
        if (item is null) return NotFound();
        if (item.InventoryId != inventoryId) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "name is required" });
        }

        if (string.IsNullOrWhiteSpace(request.CustomId))
        {
            return BadRequest(new { message = "customId is required" });
        }

        var isValid = await _customIdService.IsValidAsync(inventoryId, request.CustomId.Trim());
        if (!isValid)
        {
            return BadRequest(new { message = "customId does not match the inventory format" });
        }

        item.CustomId = request.CustomId.Trim();
        item.Name = request.Name.Trim();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            await _itemRepository.UpdateAsync(item, request.Version);
            await ReplaceItemFieldValuesAsync(inventoryId, item.Id, request.CustomValues);
            await transaction.CommitAsync();
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new { message = "Version conflict. Reload and try again." });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(new { message = "Duplicate customId in this inventory." });
        }

        var likeCount = await _itemLikeRepository.CountAsync(item.Id);
        var customValues = await GetItemFieldValuesAsync(inventoryId, item.Id);

        var response = new ItemResponse(
            item.Id,
            item.InventoryId,
            item.CustomId,
            item.Name,
            item.Version,
            likeCount,
            customValues
        );

        return Ok(response);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid inventoryId, Guid id)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanWriteItemsAsync(inventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        var item = await _itemRepository.GetByIdAsync(id);
        if (item is null) return NotFound();
        if (item.InventoryId != inventoryId) return NotFound();

        await _itemRepository.DeleteAsync(item);

        return NoContent();
    }

    [Authorize]
    [HttpPatch("{id:guid}/autosave")]
    public async Task<ActionResult<ItemResponse>> AutoSave(Guid inventoryId, Guid id, AutoSaveItemRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var isAdmin = RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanWriteItemsAsync(inventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        var item = await _itemRepository.GetByIdAsync(id);
        if (item is null) return NotFound();
        if (item.InventoryId != inventoryId) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "name is required" });
        }

        if (string.IsNullOrWhiteSpace(request.CustomId))
        {
            return BadRequest(new { message = "customId is required" });
        }

        var isValid = await _customIdService.IsValidAsync(inventoryId, request.CustomId.Trim());
        if (!isValid)
        {
            return BadRequest(new { message = "customId does not match the inventory format" });
        }

        item.CustomId = request.CustomId.Trim();
        item.Name = request.Name.Trim();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            await _itemRepository.UpdateAsync(item, request.Version);
            await ReplaceItemFieldValuesAsync(inventoryId, item.Id, request.CustomValues);
            await transaction.CommitAsync();
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new { message = "Version conflict. Reload and try again." });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(new { message = "Duplicate customId in this inventory." });
        }

        var likeCount = await _itemLikeRepository.CountAsync(item.Id);
        var customValues = await GetItemFieldValuesAsync(inventoryId, item.Id);

        return Ok(new ItemResponse(
            item.Id,
            item.InventoryId,
            item.CustomId,
            item.Name,
            item.Version,
            likeCount,
            customValues
        ));
    }
}