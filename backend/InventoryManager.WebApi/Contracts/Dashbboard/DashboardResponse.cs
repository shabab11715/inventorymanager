namespace InventoryManager.WebApi.Contracts.Dashboard;

public record DashboardResponse(
    List<DashboardInventoryCardResponse> LatestInventories,
    List<DashboardInventoryCardResponse> TopInventories,
    List<DashboardTagResponse> TagCloud
);

public record DashboardInventoryCardResponse(
    Guid Id,
    string Title,
    string Description,
    string ImageUrl,
    string CreatorName,
    int ItemCount
);

public record DashboardTagResponse(
    string Name,
    int InventoryCount
);