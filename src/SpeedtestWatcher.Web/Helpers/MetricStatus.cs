using MudBlazor;

namespace SpeedtestWatcher.Web.Helpers;

public static class MetricStatus
{
    public static Color Ping(int? value, int? target) => Resolve(value, target, higherIsBetter: false);

    public static Color Download(double? value, double? target) => Resolve(value, target, higherIsBetter: true);

    public static Color Upload(double? value, double? target) => Resolve(value, target, higherIsBetter: true);

    private static Color Resolve(double? value, double? target, bool higherIsBetter)
    {
        if (value == null || target is not > 0) return Color.Default;

        return SpeedQualityHelper.GetQuality(value.Value, target.Value, higherIsBetter) switch
        {
            SpeedQuality.Green => Color.Success,
            SpeedQuality.Orange => Color.Warning,
            SpeedQuality.Red => Color.Error,
            _ => Color.Default
        };
    }

    public const string ChartDownload = "#06b6d4";
    public const string ChartUpload = "#a855f7";
    public const string ChartPing = "#f59e0b";
    public const string ChartJitter = "#ec4899";
    public const string ChartAverage = "#94a3b8";

    public static string CssVar(Color color) => color switch
    {
        Color.Success => "var(--mud-palette-success)",
        Color.Warning => "var(--mud-palette-warning)",
        Color.Error => "var(--mud-palette-error)",
        Color.Primary => "var(--mud-palette-primary)",
        _ => "var(--mud-palette-text-primary)"
    };
}
