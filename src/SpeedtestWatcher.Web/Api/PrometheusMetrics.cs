using System.Globalization;
using System.Text;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Web.Api;

public static class PrometheusMetrics
{
    public const string ContentType = "text/plain; version=0.0.4; charset=utf-8";

    public static string Format(ResultsSummary summary)
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

    private static void Gauge(StringBuilder sb, string name, string help, double? value, string labels, string format)
    {
        if (value is not { } number) return;

        sb.AppendLine(CultureInfo.InvariantCulture, $"# HELP speedtest_watcher_{name} {help}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"# TYPE speedtest_watcher_{name} gauge");
        var labelPart = string.IsNullOrEmpty(labels) ? "" : $"{{{labels}}}";
        sb.AppendLine(CultureInfo.InvariantCulture, $"speedtest_watcher_{name}{labelPart} {number.ToString(format, CultureInfo.InvariantCulture)}");
    }

    private static string Escape(string? value) =>
        (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
}
