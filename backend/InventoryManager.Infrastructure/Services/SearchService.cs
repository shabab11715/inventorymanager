using InventoryManager.Application.Interfaces;
using InventoryManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace InventoryManager.Infrastructure.Persistence;

public class SearchService : ISearchService
{
    private readonly InventoryManagerDbContext _db;

    public SearchService(InventoryManagerDbContext db)
    {
        _db = db;
    }

    public async Task<(List<Inventory> Inventories, List<Item> Items)> SearchAsync(string query, Guid userId, bool isAdmin, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 50) pageSize = 50;

        var skip = (pageNumber - 1) * pageSize;

        var inventories = await _db.Inventories
            .AsNoTracking()
            .Where(i => EF.Property<NpgsqlTsVector>(i, "SearchVector")
                .Matches(EF.Functions.WebSearchToTsQuery("simple", query)))
            .OrderByDescending(i => EF.Property<NpgsqlTsVector>(i, "SearchVector")
                .Rank(EF.Functions.WebSearchToTsQuery("simple", query)))
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        var items = await _db.Items
            .AsNoTracking()
            .Where(t => EF.Property<NpgsqlTsVector>(t, "SearchVector")
                .Matches(EF.Functions.WebSearchToTsQuery("simple", query)))
            .OrderByDescending(t => EF.Property<NpgsqlTsVector>(t, "SearchVector")
                .Rank(EF.Functions.WebSearchToTsQuery("simple", query)))
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return (inventories, items);
    }
}