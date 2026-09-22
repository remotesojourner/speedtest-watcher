using System.Globalization;

namespace SpeedtestWatcher.Application.Common;

public static class TimeZones
{
    public static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static DateTime ParseUtcTimestamp(string value) =>
        AsUtc(DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

    public static bool TryParseUtcTimestamp(string value, out DateTime utc)
    {
        var parsed = DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var stored);
        utc = parsed ? AsUtc(stored) : default;
        return parsed;
    }

    public static DateTime InTimeZone(DateTime value, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeFromUtc(AsUtc(value), timeZone);

    public static DateTime WallClockToUtc(DateTime wallClock, TimeZoneInfo timeZone)
    {
        var local = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);
        while (timeZone.IsInvalidTime(local))
        {
            local = local.AddMinutes(15);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }

    public static bool TryFindTimeZone(string id, out TimeZoneInfo timeZone)
    {
        var found = TimeZoneInfo.TryFindSystemTimeZoneById(id, out var match);
        timeZone = match ?? TimeZoneInfo.Utc;
        return found;
    }
}
