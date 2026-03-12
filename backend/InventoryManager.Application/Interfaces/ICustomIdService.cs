using InventoryManager.Application.CustomIds;

namespace InventoryManager.Application.Interfaces;

public interface ICustomIdService
{
    Task<GeneratedCustomIdResult> GenerateAsync(Guid inventoryId);
    Task<GeneratedCustomIdResult> GeneratePreviewAsync(Guid inventoryId, string? customIdFormat);
    Task<bool> IsValidAsync(Guid inventoryId, string customId);
}