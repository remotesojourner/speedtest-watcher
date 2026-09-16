using System.Globalization;

namespace SpeedtestWatcher.Core.Helpers;

public static class FormatHelper
{
    public static DateTime StoredToLocal(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value,
        DateTimeKind.Utc => value.ToLocalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime()
    };

    public static DateTime ParseTimestamp(string value) =>
        StoredToLocal(DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

    public static bool TryParseTimestamp(string value, out DateTime local)
    {
        var parsed = DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var stored);
        local = parsed ? StoredToLocal(stored) : default;
        return parsed;
    }

    public static double ConvertSpeed(double? mbps, string speedUnit)
    {
        if (mbps == null || mbps < 0) return mbps ?? 0;
        if (speedUnit == "mbytes")
        {
            return Math.Round(mbps.Value / 8.0, 2);
        }
        return mbps.Value;
    }

    public static string FormatTime(DateTime dateTime, string timeFormat)
    {
        var use12H = timeFormat == "12h";
        return use12H ? dateTime.ToString("hh:mm tt") : dateTime.ToString("HH:mm");
    }

    public static string DatePattern(string? dateFormat) => dateFormat switch
    {
        "mdy" => "MM/dd/yyyy",
        "ymd" => "yyyy-MM-dd",
        _ => "dd/MM/yyyy"
    };

    public static string FormatDate(DateTime dateTime, string? dateFormat) =>
        dateTime.ToString(DatePattern(dateFormat), CultureInfo.InvariantCulture);

    public static string FormatDateTime(DateTime dateTime, string timeFormat, string? dateFormat = "ymd") =>
        $"{FormatDate(dateTime, dateFormat)} {FormatTime(dateTime, timeFormat)}";

    public static string ToRelativeTime(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime.ToUniversalTime();

        if (span.TotalSeconds < 60)
            return "Just now";
        if (span.TotalMinutes < 60)
        {
            var mins = Math.Max(1, (int)span.TotalMinutes);
            return mins == 1 ? "1 minute ago" : $"{mins} minutes ago";
        }
        if (span.TotalHours < 24)
        {
            var hours = (int)span.TotalHours;
            return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
        }
        var days = (int)span.TotalDays;
        return days == 1 ? "1 day ago" : $"{days} days ago";
    }
}
