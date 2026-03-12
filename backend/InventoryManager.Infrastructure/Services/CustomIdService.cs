using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using InventoryManager.Application.CustomIds;
using InventoryManager.Application.Interfaces;

namespace InventoryManager.Infrastructure.Services;

public class CustomIdService : ICustomIdService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IInventoryRepository _inventories;
    private readonly IItemRepository _items;

    public CustomIdService(IInventoryRepository inventories, IItemRepository items)
    {
        _inventories = inventories;
        _items = items;
    }

    public async Task<GeneratedCustomIdResult> GenerateAsync(Guid inventoryId)
    {
        var inventory = await _inventories.GetByIdAsync(inventoryId);
        if (inventory is null)
        {
            throw new InvalidOperationException("Inventory not found.");
        }

        return await GenerateFromFormatAsync(inventoryId, inventory.CustomIdFormat);
    }

    public async Task<GeneratedCustomIdResult> GeneratePreviewAsync(Guid inventoryId, string? customIdFormat)
    {
        return await GenerateFromFormatAsync(inventoryId, customIdFormat);
    }

    public async Task<bool> IsValidAsync(Guid inventoryId, string customId)
    {
        if (string.IsNullOrWhiteSpace(customId))
        {
            return false;
        }

        var inventory = await _inventories.GetByIdAsync(inventoryId);
        if (inventory is null)
        {
            return false;
        }

        var elements = ParseFormat(inventory.CustomIdFormat);
        var regex = BuildRegex(elements);

        return Regex.IsMatch(customId, regex, RegexOptions.CultureInvariant);
    }

    private async Task<GeneratedCustomIdResult> GenerateFromFormatAsync(Guid inventoryId, string? customIdFormat)
    {
        var sequenceNumber = await _items.GetNextSequenceNumberAsync(inventoryId);
        var elements = ParseFormat(customIdFormat);

        var sb = new StringBuilder();

        foreach (var element in elements)
        {
            sb.Append(GeneratePart(element, sequenceNumber));
        }

        return new GeneratedCustomIdResult(sb.ToString(), sequenceNumber);
    }

    private static List<CustomIdFormatElement> ParseFormat(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
        {
            return new List<CustomIdFormatElement>
            {
                new() { Type = "sequence", PadLength = 4 }
            };
        }

        var elements = JsonSerializer.Deserialize<List<CustomIdFormatElement>>(json, JsonOptions);

        if (elements is null || elements.Count == 0)
        {
            return new List<CustomIdFormatElement>
            {
                new() { Type = "sequence", PadLength = 4 }
            };
        }

        return elements;
    }

    private static string GeneratePart(CustomIdFormatElement element, int sequenceNumber)
    {
        var type = (element.Type ?? string.Empty).Trim().ToLowerInvariant();

        return type switch
        {
            "fixedtext" => element.Value ?? string.Empty,
            "fixed" => element.Value ?? string.Empty,
            "random20" => RandomUInt32(0, (1 << 20) - 1).ToString(),
            "random32" => RandomUInt32(0, int.MaxValue).ToString(),
            "random6" => RandomDigits(6),
            "random9" => RandomDigits(9),
            "guid" => Guid.NewGuid().ToString(element.Format ?? "N"),
            "datetime" => DateTime.UtcNow.ToString(element.Format ?? "yyyyMMddHHmmss"),
            "date" => DateTime.UtcNow.ToString(element.Format ?? "yyyyMMdd"),
            "sequence" => FormatSequence(sequenceNumber, element.PadLength),
            _ => throw new InvalidOperationException($"Unsupported custom ID element type: {element.Type}")
        };
    }

    private static string BuildRegex(List<CustomIdFormatElement> elements)
    {
        var sb = new StringBuilder("^");

        foreach (var element in elements)
        {
            var type = (element.Type ?? string.Empty).Trim().ToLowerInvariant();

            sb.Append(type switch
            {
                "fixedtext" => Regex.Escape(element.Value ?? string.Empty),
                "fixed" => Regex.Escape(element.Value ?? string.Empty),
                "random20" => @"\d+",
                "random32" => @"\d+",
                "random6" => @"\d{6}",
                "random9" => @"\d{9}",
                "guid" => BuildGuidRegex(element.Format),
                "datetime" => BuildDateRegex(element.Format ?? "yyyyMMddHHmmss"),
                "date" => BuildDateRegex(element.Format ?? "yyyyMMdd"),
                "sequence" => BuildSequenceRegex(element.PadLength),
                _ => throw new InvalidOperationException($"Unsupported custom ID element type: {element.Type}")
            });
        }

        sb.Append("$");
        return sb.ToString();
    }

    private static string BuildGuidRegex(string? format)
    {
        var normalized = (format ?? "N").Trim().ToUpperInvariant();

        return normalized switch
        {
            "N" => "[A-Fa-f0-9]{32}",
            "D" => "[A-Fa-f0-9]{8}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{12}",
            _ => "[A-Fa-f0-9\\-]{32,36}"
        };
    }

    private static string BuildDateRegex(string format)
    {
        return Regex.Escape(format)
            .Replace("yyyy", "\\d{4}")
            .Replace("MM", "\\d{2}")
            .Replace("dd", "\\d{2}")
            .Replace("HH", "\\d{2}")
            .Replace("mm", "\\d{2}")
            .Replace("ss", "\\d{2}");
    }

    private static string BuildSequenceRegex(int? padLength)
    {
        if (padLength is null || padLength <= 0)
        {
            return "\\d+";
        }

        return $"\\d{{{padLength.Value},}}";
    }

    private static string FormatSequence(int value, int? padLength)
    {
        if (padLength is null || padLength <= 0)
        {
            return value.ToString();
        }

        return value.ToString($"D{padLength.Value}");
    }

    private static string RandomDigits(int length)
    {
        var chars = new char[length];

        for (var i = 0; i < length; i++)
        {
            chars[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(chars);
    }

    private static int RandomUInt32(int minInclusive, int maxInclusive)
    {
        return RandomNumberGenerator.GetInt32(minInclusive, maxInclusive + 1);
    }
}