namespace InventoryManager.WebApi.Contracts.Users;

public record UserInventoryCardResponse(
    Guid Id,
    string Title,
    string Description,
    string ImageUrl,
    string CategoryName,
    int ItemCount
);

public record UserProfileResponse(
    Guid Id,
    string Name,
    string Email,
    List<UserInventoryCardResponse> OwnedInventories,
    List<UserInventoryCardResponse> WritableInventories
);