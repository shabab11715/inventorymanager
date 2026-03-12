namespace InventoryManager.WebApi.Contracts.Discussions;

public record DiscussionResponse(
    Guid Id,
    Guid InventoryId,
    Guid UserId,
    string UserName,
    string Content,
    DateTime CreatedAt
);