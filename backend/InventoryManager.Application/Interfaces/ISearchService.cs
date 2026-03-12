using InventoryManager.Domain.Entities;

namespace InventoryManager.Application.Interfaces;

public interface ISearchService
{
    Task<(List<Inventory> Inventories, List<Item> Items)> SearchAsync(string query, Guid userId, bool isAdmin, int pageNumber, int pageSize);
}