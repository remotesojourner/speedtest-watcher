using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpeedtestWatcher.Application.Integrations;

public static class IntegrationSettingsRules
{
    private const int MaxTextLength = 255;
    private const int MaxTextareaLength = 2000;

    private static readonly TimeSpan _regexTimeout = TimeSpan.FromMilliseconds(200);

    public static string? ProblemWith(IntegrationTypeSchemaDto schema, IReadOnlyDictionary<string, JsonElement> settings, IEnumerable<string> changedKeys)
    {
        var fieldNames = schema.Fields.Select(field => field.Name).ToHashSet(StringComparer.Ordinal);
        if (changedKeys.FirstOrDefault(key => !fieldNames.Contains(key)) is { } unknown)
            return $"{unknown} isn't a setting of the {schema.Title} integration";

        return schema.Fields
            .Select(field => ProblemWith(field, settings.TryGetValue(field.Name, out var value) ? value : default))
            .FirstOrDefault(problem => problem != null);
    }

    private static string? ProblemWith(IntegrationFieldSchemaDto field, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return field.Required ? $"{field.Name} is required" : null;

        return field.Type switch
        {
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : $"{field.Name} needs to be true or false",
            "number" => IsWholeNumber(value) ? null : $"{field.Name} needs to be a whole number",
            _ => TextProblem(field, value)
        };
    }

    private static bool IsWholeNumber(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number => value.TryGetInt32(out _),
        JsonValueKind.String => int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        _ => false
    };

    private static string? TextProblem(IntegrationFieldSchemaDto field, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String) return $"{field.Name} needs to be text";

        var text = value.GetString()!;
        if (text.Length == 0) return field.Required ? $"{field.Name} is required" : null;

        var maxLength = field.Type == "textarea" ? MaxTextareaLength : MaxTextLength;
        if (text.Length > maxLength) return $"{field.Name} can be at most {maxLength} characters";

        return MatchesPattern(field.Regex, text) ? null : $"{field.Name} doesn't have the expected format";
    }

    private static bool MatchesPattern(string? pattern, string text)
    {
        if (string.IsNullOrEmpty(pattern)) return true;

        try
        {
            return Regex.IsMatch(text, pattern, RegexOptions.None, _regexTimeout);
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
        {
            return true;
        }
    }
}
