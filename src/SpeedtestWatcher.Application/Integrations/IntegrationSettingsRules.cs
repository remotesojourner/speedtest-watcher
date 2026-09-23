using SpeedtestWatcher.Application.Resources;
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
            return ApplicationStrings.Format(ApplicationStrings.IntegrationUnknownSetting, unknown, schema.Title);

        return schema.Fields
            .Select(field => ProblemWith(field, settings.TryGetValue(field.Name, out var value) ? value : default))
            .FirstOrDefault(problem => problem != null);
    }

    private static string? ProblemWith(IntegrationFieldSchemaDto field, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return field.Required ? ApplicationStrings.Format(ApplicationStrings.IntegrationFieldRequired, field.Name) : null;

        return field.Type switch
        {
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : ApplicationStrings.Format(ApplicationStrings.IntegrationFieldBoolean, field.Name),
            "number" => IsWholeNumber(value) ? null : ApplicationStrings.Format(ApplicationStrings.IntegrationFieldNumber, field.Name),
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
        if (value.ValueKind != JsonValueKind.String) return ApplicationStrings.Format(ApplicationStrings.IntegrationFieldText, field.Name);

        var text = value.GetString()!;
        if (text.Length == 0) return field.Required ? ApplicationStrings.Format(ApplicationStrings.IntegrationFieldRequired, field.Name) : null;

        var maxLength = field.Type == "textarea" ? MaxTextareaLength : MaxTextLength;
        if (text.Length > maxLength) return ApplicationStrings.Format(ApplicationStrings.IntegrationFieldTooLong, field.Name, maxLength);

        return MatchesPattern(field.Regex, text) ? null : ApplicationStrings.Format(ApplicationStrings.IntegrationFieldFormat, field.Name);
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
