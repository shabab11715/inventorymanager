namespace InventoryManager.WebApi.Contracts.Users;

public record UserAutocompleteResponse(Guid Id, string Name, string Email);