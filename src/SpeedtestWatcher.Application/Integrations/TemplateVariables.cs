using System.Globalization;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

internal static class TemplateVariables
{
    public static Dictionary<string, string> For(IntegrationEvent integrationEvent) => integrationEvent switch
    {
        TestFinished finished => From(finished.Result),
        TestUnhealthy unhealthy => From(unhealthy.Result),
        TestHealthyAgain healthyAgain => From(healthyAgain.Result),
        TestFailed failed => From(failed.Result),
        TestSkipped skipped => From(skipped.Result),
        ConnectionLost lost => new(StringComparer.OrdinalIgnoreCase)
        {
            ["since"] = lost.Since.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC"
        },
        ConnectionRestored restored => new(StringComparer.OrdinalIgnoreCase)
        {
            ["since"] = (restored.At - restored.Downtime).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC",
            ["downtime"] = Duration.Describe(restored.Downtime)
        },
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
        ["threshold_upload"] = Decimal(test.ThresholdUpload) ?? "-",
        ["missed"] = TargetSettings.Describe(Missed(test)),
        ["packet_loss"] = Decimal(test.PacketLoss) ?? "-",
        ["bufferbloat_down"] = Decimal(test.BufferbloatDown) ?? "-",
        ["bufferbloat_up"] = Decimal(test.BufferbloatUp) ?? "-",
        ["data_used"] = test.DownloadBytes + test.UploadBytes is { } bytes ? ByteSize.Describe(bytes) : "-"
    };

    private static IReadOnlyList<TargetKind> Missed(Speedtest test) =>
        test.Status == TestStatus.Completed
            ? new TargetSettings(test.ThresholdPing, test.ThresholdDownload, test.ThresholdUpload, test.ThresholdPacketLoss, test.ThresholdBufferbloat)
                .Missed(new Readings(test.Ping, test.Download, test.Upload, test.PacketLoss, test.BufferbloatDown, test.BufferbloatUp))
            : [];

    private static string? Decimal(double? value) => value?.ToString("F2", CultureInfo.InvariantCulture);
}
