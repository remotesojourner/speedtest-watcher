using System.Globalization;

namespace SpeedtestWatcher.Application.Common;

public static class Duration
{
    public static string Describe(TimeSpan length)
    {
        var seconds = (long)Math.Round(Math.Max(length.TotalSeconds, 0));
        if (seconds < 60) return Count(seconds, "second");
        if (seconds < 3600) return Join(Count(seconds / 60, "minute"), seconds % 60, "second");
        if (seconds < 86400) return Join(Count(seconds / 3600, "hour"), seconds % 3600 / 60, "minute");

        return Join(Count(seconds / 86400, "day"), seconds % 86400 / 3600, "hour");
    }

    private static string Join(string whole, long rest, string unit) =>
        rest == 0 ? whole : $"{whole} {Count(rest, unit)}";

    private static string Count(long value, string unit) =>
        string.Create(CultureInfo.InvariantCulture, $"{value} {unit}{(value == 1 ? "" : "s")}");
}
