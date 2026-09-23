using SpeedtestWatcher.Application.Resources;

namespace SpeedtestWatcher.Application.Common;

public static class Duration
{
    public static string Describe(TimeSpan length)
    {
        var seconds = (long)Math.Round(Math.Max(length.TotalSeconds, 0));
        if (seconds < 60) return Seconds(seconds);
        if (seconds < 3600) return Join(Minutes(seconds / 60), seconds % 60, Seconds);
        if (seconds < 86400) return Join(Hours(seconds / 3600), seconds % 3600 / 60, Minutes);

        return Join(Days(seconds / 86400), seconds % 86400 / 3600, Hours);
    }

    private static string Join(string whole, long rest, Func<long, string> describe) =>
        rest == 0 ? whole : $"{whole} {describe(rest)}";

    private static string Seconds(long value) =>
        value == 1 ? ApplicationStrings.DurationOneSecond : ApplicationStrings.Format(ApplicationStrings.DurationSeconds, value);

    private static string Minutes(long value) =>
        value == 1 ? ApplicationStrings.DurationOneMinute : ApplicationStrings.Format(ApplicationStrings.DurationMinutes, value);

    private static string Hours(long value) =>
        value == 1 ? ApplicationStrings.DurationOneHour : ApplicationStrings.Format(ApplicationStrings.DurationHours, value);

    private static string Days(long value) =>
        value == 1 ? ApplicationStrings.DurationOneDay : ApplicationStrings.Format(ApplicationStrings.DurationDays, value);
}
