using InventoryManager.Application.Interfaces;
using InventoryManager.WebApi.Auth;
using InventoryManager.WebApi.Contracts.Search;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManager.WebApi.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet]
    public async Task<ActionResult<SearchResultResponse>> Search(
        [FromQuery] string q,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new SearchResultResponse(new List<SearchInventoryHit>(), new List<SearchItemHit>()));
        }

        var userId = User.Identity?.IsAuthenticated == true ? UserContext.GetUserId(User) : Guid.Empty;
        var isAdmin = User.Identity?.IsAuthenticated == true && RoleContext.IsAdmin(User);

        var (inventories, items) = await _searchService.SearchAsync(q, userId, isAdmin, pageNumber, pageSize);

        var invHits = inventories.Select(x => new SearchInventoryHit(x.Id, x.Title, x.Description, x.ImageUrl)).ToList();
        var itemHits = items.Select(x => new SearchItemHit(x.Id, x.InventoryId, x.CustomId, x.Name)).ToList();

        return Ok(new SearchResultResponse(invHits, itemHits));
    }
}