namespace InventoryManager.Application.CustomIds;

public class CustomIdFormatElement
{
    public string Type { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Format { get; set; }
    public int? Length { get; set; }
    public int? PadLength { get; set; }
}