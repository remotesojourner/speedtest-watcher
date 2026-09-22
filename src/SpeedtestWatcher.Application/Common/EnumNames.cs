using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeedtestWatcher.Application.Common;

public static class EnumNames
{
    public static string ToName<TEnum>(this TEnum value) where TEnum : struct, Enum =>
        typeof(TEnum).GetField(value.ToString())?.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name
        ?? JsonNamingPolicy.CamelCase.ConvertName(value.ToString());

    public static bool TryParse<TEnum>([NotNullWhen(true)] string? name, out TEnum value) where TEnum : struct, Enum
    {
        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (string.Equals(candidate.ToName(), name, StringComparison.OrdinalIgnoreCase))
            {
                value = candidate;
                return true;
            }
        }

        value = default;
        return false;
    }
}
