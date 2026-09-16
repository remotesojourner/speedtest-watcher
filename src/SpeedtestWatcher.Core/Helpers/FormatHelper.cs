using System.Globalization;

namespace SpeedtestWatcher.Core.Helpers;

public static class FormatHelper
{
    // The database stores UTC wall-clock times without a zone marker, so a value without a Kind is UTC.
    public static DateTime StoredToLocal(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value,
        DateTimeKind.Utc => value.ToLocalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime()
    };

    public static DateTime ParseTimestamp(string value) =>
        StoredToLocal(DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

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
        bool use12h = timeFormat == "12h";
        return use12h ? dateTime.ToString("hh:mm tt") : dateTime.ToString("HH:mm");
    }

    /// <summary>Date layout for the "dmy", "mdy" and "ymd" presets.</summary>
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
            int mins = Math.Max(1, (int)span.TotalMinutes);
            return mins == 1 ? "1 minute ago" : $"{mins} minutes ago";
        }
        if (span.TotalHours < 24)
        {
            int hours = (int)span.TotalHours;
            return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
        }
        int days = (int)span.TotalDays;
        return days == 1 ? "1 day ago" : $"{days} days ago";
    }
}
