using MudBlazor;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Web.Helpers;

/// <summary>
/// One place that decides what color a ping/download/upload reading gets, so the same
/// metric is colored identically everywhere it appears (history hero, test list rows,
/// test detail dialog) instead of each page inventing its own rule.
/// </summary>
public static class MetricStatus
{
    public static Color Ping(int? value, string? optimalRaw) => Resolve(value, optimalRaw, higherIsBetter: false);

    public static Color Download(double? value, string? optimalRaw) => Resolve(value, optimalRaw, higherIsBetter: true);

    public static Color Upload(double? value, string? optimalRaw) => Resolve(value, optimalRaw, higherIsBetter: true);

    private static Color Resolve(double? value, string? optimalRaw, bool higherIsBetter)
    {
        if (value == null) return Color.Default;
        if (!double.TryParse(optimalRaw, out var optimal) || optimal <= 0) return Color.Default;

        return SpeedQualityHelper.GetQuality(value.Value, optimal, higherIsBetter) switch
        {
            SpeedQuality.Green => Color.Success,
            SpeedQuality.Orange => Color.Warning,
            SpeedQuality.Red => Color.Error,
            _ => Color.Default
        };
    }

    // Mirrors wwwroot/css/speedtest-watcher.css --chart-* tokens. MudChart reads colors from
    // ChartOptions.ChartPalette, not from CSS custom properties, so the hex values are
    // duplicated here deliberately to keep chart series and CSS-driven UI in sync.
    // Each MudChart instance indexes its own series from 0, so callers pass the single-color
    // or two-color array matching that specific chart's series order (see Dashboard.razor).
    public const string ChartDownload = "#06b6d4";
    public const string ChartUpload = "#a855f7";
    public const string ChartPing = "#f59e0b";
    public const string ChartJitter = "#ec4899";
    // Neutral, so the reference line never reads as another metric.
    public const string ChartAverage = "#94a3b8";

    /// <summary>CSS variable backing a MudBlazor semantic <see cref="Color"/>, for use in raw inline styles/SVG.</summary>
    public static string CssVar(Color color) => color switch
    {
        Color.Success => "var(--mud-palette-success)",
        Color.Warning => "var(--mud-palette-warning)",
        Color.Error => "var(--mud-palette-error)",
        Color.Primary => "var(--mud-palette-primary)",
        _ => "var(--mud-palette-text-primary)"
    };
}
