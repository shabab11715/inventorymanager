namespace InventoryManager.WebApi.Contracts.Users;

public record AdminUserResponse(
    Guid Id,
    string Email,
    string Name,
    string Role,
    bool IsBlocked,
    DateTime CreatedAt
);