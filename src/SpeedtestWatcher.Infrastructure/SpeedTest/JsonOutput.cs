using System.Text.Json;

namespace SpeedtestWatcher.Infrastructure.SpeedTest;

internal static class JsonOutput
{
    public static T? FromLastLine<T>(string output, Func<JsonElement, T?> read) where T : class
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).Reverse();
        foreach (var line in lines)
        {
            if (!line.StartsWith('{') && !line.StartsWith('[')) continue;

            using var document = TryParse(line);
            if (document != null && read(document.RootElement) is { } value) return value;
        }

        return null;
    }

    public static JsonElement FirstIfArray(JsonElement root) =>
        root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0 ? root[0] : root;

    public static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null
            }
            : null;

    public static double? Number(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetDouble(out var number)
            ? number
            : null;

    private static JsonDocument? TryParse(string line)
    {
        try
        {
            return JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
