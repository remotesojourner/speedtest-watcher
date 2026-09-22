using System.Globalization;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Web.Ui.Display;

public static class DisplayFormat
{
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
        return dateTime.ToString(use12H ? "hh:mm tt" : "HH:mm", CultureInfo.CurrentCulture);
    }

    public static string DatePattern(string? dateFormat) => dateFormat switch
    {
        "mdy" => "MM/dd/yyyy",
        "ymd" => "yyyy-MM-dd",
        _ => "dd/MM/yyyy"
    };

    public static string DayAndMonthPattern(string? dateFormat) => dateFormat switch
    {
        "mdy" => "MM/dd",
        "ymd" => "MM-dd",
        _ => "dd/MM"
    };

    public static string FormatDate(DateTime dateTime, string? dateFormat) =>
        dateTime.ToString(DatePattern(dateFormat), CultureInfo.InvariantCulture);

    public static string FormatDateTime(DateTime dateTime, string timeFormat, string? dateFormat = "ymd") =>
        $"{FormatDate(dateTime, dateFormat)} {FormatTime(dateTime, timeFormat)}";

    public static string ToRelativeTime(DateTime moment)
    {
        var span = DateTime.UtcNow - TimeZones.AsUtc(moment);

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
