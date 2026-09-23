using System.Globalization;
using System.Text;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Web.Api;

public static class PrometheusMetrics
{
    public const string ContentType = "text/plain; version=0.0.4; charset=utf-8";

    public static string Format(ResultsSummary summary, DataUsedDto dataUsed, MonitoringStatusDto monitoring)
    {
        var completed = summary.LatestCompleted;
        var latest = summary.Latest;
        var completedLabels = Labels(completed);
        var latestLabels = Labels(latest);
        var sb = new StringBuilder();

        Gauge(sb, "ping", "Ping of the latest completed test in ms", completed?.Ping, completedLabels, "F0");
        Gauge(sb, "jitter", "Jitter of the latest completed test in ms", completed?.Jitter, completedLabels, "F2");
        Gauge(sb, "download", "Download speed of the latest completed test in Mbps", completed?.Download, completedLabels, "F2");
        Gauge(sb, "upload", "Upload speed of the latest completed test in Mbps", completed?.Upload, completedLabels, "F2");
        Gauge(sb, "time", "Duration of the latest completed test in seconds", completed?.Time, completedLabels, "F0");
        Gauge(sb, "packet_loss", "Packet loss of the latest completed test as a percentage, when the provider measured it", completed?.PacketLoss, completedLabels, "F2");
        Gauge(sb, "bufferbloat_download_ms", "How much longer the line took to answer while the latest completed test was downloading than when idle, in ms", completed?.BufferbloatDown, completedLabels, "F2");
        Gauge(sb, "bufferbloat_upload_ms", "How much longer the line took to answer while the latest completed test was uploading than when idle, in ms", completed?.BufferbloatUp, completedLabels, "F2");
        Gauge(sb, "last_test_bytes", "Bytes the latest completed test moved, download and upload together, when the provider reported them", BytesMoved(completed), completedLabels, "F0");
        GaugeFamily(sb, "data_used_bytes", "Bytes the tests in a period moved, download and upload together", "F0",
        [
            ("period=\"24h\"", dataUsed.Last24Hours),
            ("period=\"7d\"", dataUsed.Last7Days),
            ("period=\"30d\"", dataUsed.Last30Days),
            ("period=\"stored\"", dataUsed.Stored)
        ]);

        Gauge(sb, "healthy", "Whether the latest completed test met its targets (1 healthy, 0 not)",
            completed?.Healthy is { } healthy ? (healthy ? 1 : 0) : null, completedLabels, "F0");
        Gauge(sb, "threshold_ping", "Ping target the latest completed test was judged against, in ms", completed?.ThresholdPing, completedLabels, "F0");
        Gauge(sb, "threshold_download", "Download target the latest completed test was judged against, in Mbps", completed?.ThresholdDownload, completedLabels, "F2");
        Gauge(sb, "threshold_upload", "Upload target the latest completed test was judged against, in Mbps", completed?.ThresholdUpload, completedLabels, "F2");

        Gauge(sb, "last_test_timestamp_seconds", "When the latest test ran, in Unix seconds", UnixSeconds(latest), latestLabels, "F0");
        Gauge(sb, "last_completed_test_timestamp_seconds", "When the latest completed test ran, in Unix seconds", UnixSeconds(completed), completedLabels, "F0");
        Gauge(sb, "tests_total", "Number of results stored", summary.Total, "", "F0");

        Gauge(sb, "server", "Server id of the latest completed test", completed?.ServerId ?? 0, "", "F0");
        Gauge(sb, "server_info", "Static info about the latest completed test (always 1).", 1, completedLabels, "F0");

        Gauge(sb, "connection_up", "Whether the connection monitor last saw the line up (1 up, 0 down)", UpOrDown(monitoring.Health), "", "F0");
        Gauge(sb, "connection_latency_ms", "Quickest answer from a probe target in the latest round, in ms", monitoring.FastestMilliseconds, "", "F2");
        Gauge(sb, "outages_total", "Outages the monitor recorded in the last 30 days", monitoring.Last30Days.Outages, "", "F0");
        Gauge(sb, "outage_seconds_total", "Seconds the line was down in the last 30 days", monitoring.Last30Days.DownSeconds, "", "F0");
        GaugeFamily(sb, "uptime_percent", "Share of the watched time the line was up, as a percentage", "F3",
        [
            ("period=\"24h\"", monitoring.Last24Hours.Percent),
            ("period=\"7d\"", monitoring.Last7Days.Percent)
        ]);

        return sb.ToString();
    }

    private static long? UnixSeconds(Speedtest? test) =>
        test == null ? null : new DateTimeOffset(test.Created).ToUnixTimeSeconds();

    private static string Labels(Speedtest? test) =>
        string.Join(",",
            $"server_id=\"{test?.ServerId ?? 0}\"",
            $"server_name=\"{Escape(test?.ServerName)}\"",
            $"server_host=\"{Escape(test?.ServerHost)}\"",
            $"status=\"{Escape(test?.Status.ToName() ?? "none")}\"",
            $"scheduled=\"{(test?.Type == TestType.Custom ? "false" : "true")}\"");

    private static long? BytesMoved(Speedtest? test) =>
        test == null || (test.DownloadBytes == null && test.UploadBytes == null)
            ? null
            : (test.DownloadBytes ?? 0) + (test.UploadBytes ?? 0);

    private static void GaugeFamily(StringBuilder sb, string name, string help, string format, IReadOnlyList<(string Labels, double? Value)> series)
    {
        if (series.All(point => point.Value == null)) return;

        Describe(sb, name, help);
        foreach (var (labels, value) in series)
        {
            if (value is not { } number) continue;

            sb.AppendLine(CultureInfo.InvariantCulture, $"speedtest_watcher_{name}{{{labels}}} {number.ToString(format, CultureInfo.InvariantCulture)}");
        }
    }

    private static double? UpOrDown(ConnectionHealth health) => health switch
    {
        ConnectionHealth.Up => 1,
        ConnectionHealth.Down => 0,
        _ => null
    };

    private static void Describe(StringBuilder sb, string name, string help)
    {
        sb.AppendLine(CultureInfo.InvariantCulture, $"# HELP speedtest_watcher_{name} {help}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"# TYPE speedtest_watcher_{name} gauge");
    }

    private static void Gauge(StringBuilder sb, string name, string help, double? value, string labels, string format)
    {
        if (value is not { } number) return;

        Describe(sb, name, help);
        var labelPart = string.IsNullOrEmpty(labels) ? "" : $"{{{labels}}}";
        sb.AppendLine(CultureInfo.InvariantCulture, $"speedtest_watcher_{name}{labelPart} {number.ToString(format, CultureInfo.InvariantCulture)}");
    }

    private static string Escape(string? value) =>
        (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
}
