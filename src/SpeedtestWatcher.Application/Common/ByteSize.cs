using System.Globalization;

namespace SpeedtestWatcher.Application.Common;

public static class ByteSize
{
    private const double Kilobyte = 1024;
    private const double Megabyte = Kilobyte * 1024;
    private const double Gigabyte = Megabyte * 1024;
    private const double Terabyte = Gigabyte * 1024;

    public static string Describe(long bytes) => bytes switch
    {
        < (long)Kilobyte => $"{bytes} B",
        < (long)Megabyte => Format(bytes / Kilobyte, "KB"),
        < (long)Gigabyte => Format(bytes / Megabyte, "MB"),
        < (long)Terabyte => Format(bytes / Gigabyte, "GB"),
        _ => Format(bytes / Terabyte, "TB")
    };

    private static string Format(double value, string unit) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:F1} {unit}");
}
