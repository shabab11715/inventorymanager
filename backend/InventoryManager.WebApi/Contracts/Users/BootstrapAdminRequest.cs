namespace InventoryManager.WebApi.Contracts.Users;

public record BootstrapAdminRequest(
    string Email,
    string SetupKey
);