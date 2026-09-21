using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SpeedtestWatcher.Core.Helpers;

public static class EnumNames
{
    public static string ToName<TEnum>(this TEnum value) where TEnum : struct, Enum =>
        JsonNamingPolicy.CamelCase.ConvertName(value.ToString());

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
