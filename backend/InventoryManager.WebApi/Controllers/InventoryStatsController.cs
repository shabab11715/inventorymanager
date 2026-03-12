using InventoryManager.Application.Interfaces;
using InventoryManager.Infrastructure.Persistence;
using InventoryManager.WebApi.Auth;
using InventoryManager.WebApi.Contracts.Stats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.WebApi.Controllers;

[ApiController]
[Route("api/inventories/{inventoryId:guid}/stats")]
public class InventoryStatsController : ControllerBase
{
    private readonly InventoryManagerDbContext _db;
    private readonly IInventoryAccessService _inventoryAccessService;

    public InventoryStatsController(
        InventoryManagerDbContext db,
        IInventoryAccessService inventoryAccessService)
    {
        _db = db;
        _inventoryAccessService = inventoryAccessService;
    }

    [HttpGet]
    public async Task<ActionResult<InventoryStatsResponse>> Get(Guid inventoryId)
    {
        var userId = User.Identity?.IsAuthenticated == true ? UserContext.GetUserId(User) : Guid.Empty;
        var isAdmin = User.Identity?.IsAuthenticated == true && RoleContext.IsAdmin(User);

        if (!await _inventoryAccessService.CanReadAsync(inventoryId, userId, isAdmin))
        {
            return Forbid();
        }

        var itemIds = await _db.Items
            .AsNoTracking()
            .Where(x => x.InventoryId == inventoryId)
            .Select(x => x.Id)
            .ToListAsync();

        var itemCount = itemIds.Count;

        var totalLikes = itemIds.Count == 0
            ? 0
            : await _db.ItemLikes
                .AsNoTracking()
                .CountAsync(x => itemIds.Contains(x.ItemId));

        var numericRows = itemIds.Count == 0
            ? new List<NumericRow>()
            : await _db.ItemFieldValues
                .AsNoTracking()
                .Where(x => itemIds.Contains(x.ItemId) && x.NumberValue.HasValue)
                .Join(
                    _db.ItemFieldDefinitions.AsNoTracking().Where(x => x.InventoryId == inventoryId),
                    value => value.FieldDefinitionId,
                    field => field.Id,
                    (value, field) => new NumericRow(field.Id, field.Title, value.NumberValue!.Value)
                )
                .ToListAsync();

        var numericFields = numericRows
            .GroupBy(x => new { x.FieldDefinitionId, x.Title })
            .Select(group => new InventoryNumericFieldStatsResponse(
                group.Key.FieldDefinitionId,
                group.Key.Title,
                group.Count(),
                group.Min(x => x.Value),
                group.Max(x => x.Value),
                group.Average(x => x.Value)
            ))
            .OrderBy(x => x.Title)
            .ToList();

        var textRows = itemIds.Count == 0
            ? new List<TextRow>()
            : await _db.ItemFieldValues
                .AsNoTracking()
                .Where(x =>
                    itemIds.Contains(x.ItemId) &&
                    (
                        (!string.IsNullOrWhiteSpace(x.StringValue)) ||
                        (!string.IsNullOrWhiteSpace(x.TextValue))
                    ))
                .Join(
                    _db.ItemFieldDefinitions.AsNoTracking()
                        .Where(x => x.InventoryId == inventoryId && (x.FieldType == "string" || x.FieldType == "text")),
                    value => value.FieldDefinitionId,
                    field => field.Id,
                    (value, field) => new TextRow(
                        field.Id,
                        field.Title,
                        !string.IsNullOrWhiteSpace(value.StringValue) ? value.StringValue! : value.TextValue!
                    )
                )
                .ToListAsync();

        var textFields = textRows
            .GroupBy(x => new { x.FieldDefinitionId, x.Title })
            .Select(group => new InventoryTextFieldStatsResponse(
                group.Key.FieldDefinitionId,
                group.Key.Title,
                group.GroupBy(x => x.Value.Trim())
                    .Select(valueGroup => new InventoryTopValueResponse(valueGroup.Key, valueGroup.Count()))
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Value)
                    .Take(5)
                    .ToList()
            ))
            .OrderBy(x => x.Title)
            .ToList();

        return Ok(new InventoryStatsResponse(
            itemCount,
            totalLikes,
            numericFields,
            textFields
        ));
    }

    private sealed record NumericRow(Guid FieldDefinitionId, string Title, double Value);
    private sealed record TextRow(Guid FieldDefinitionId, string Title, string Value);
}