using System.Globalization;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

internal static class TemplateVariables
{
    public static Dictionary<string, string> For(IntegrationEvent integrationEvent) => integrationEvent switch
    {
        TestFinished finished => From(finished.Result),
        TestUnhealthy unhealthy => From(unhealthy.Result),
        TestFailed failed => From(failed.Result),
        TestSkipped skipped => From(skipped.Result),
        _ => new(StringComparer.OrdinalIgnoreCase)
    };

    private static Dictionary<string, string> From(Speedtest test) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["ping"] = test.Ping.ToString(CultureInfo.InvariantCulture),
        ["jitter"] = Decimal(test.Jitter) ?? "0",
        ["download"] = Decimal(test.Download)!,
        ["upload"] = Decimal(test.Upload)!,
        ["error"] = test.Error ?? string.Empty,
        ["status"] = test.Status.ToName(),
        ["healthy"] = test.Healthy switch { true => "yes", false => "no", null => "unknown" },
        ["server"] = test.ServerName ?? string.Empty,
        ["threshold_ping"] = test.ThresholdPing?.ToString(CultureInfo.InvariantCulture) ?? "-",
        ["threshold_download"] = Decimal(test.ThresholdDownload) ?? "-",
        ["threshold_upload"] = Decimal(test.ThresholdUpload) ?? "-"
    };

    private static string? Decimal(double? value) => value?.ToString("F2", CultureInfo.InvariantCulture);
}
