using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Web.Api;

namespace SpeedtestWatcher.IntegrationTests.Web.Api;

public sealed partial class GrafanaDashboardTests
{
    private static readonly JsonDocument _dashboard = JsonDocument.Parse(File.ReadAllText(Beside("../../../../docs/grafana-dashboard.json")));

    [Fact]
    public void EveryMetricTheDashboardDrawsIsOneTheAppCanServe()
    {
        var served = MetricName().Matches(EverythingMeasured()).Select(match => match.Value).ToHashSet(StringComparer.Ordinal);

        var missing = Expressions()
            .SelectMany(expression => MetricName().Matches(expression).Select(match => match.Value))
            .Distinct(StringComparer.Ordinal)
            .Where(metric => !served.Contains(metric))
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void ThePanelsCarryWhatGrafanaNeedsToImportThem()
    {
        var root = _dashboard.RootElement;
        var panels = root.GetProperty("panels").EnumerateArray().ToList();

        Assert.Equal("Speedtest Watcher", root.GetProperty("title").GetString());
        Assert.Equal("DS_PROMETHEUS", root.GetProperty("__inputs")[0].GetProperty("name").GetString());
        Assert.NotEmpty(panels);
        Assert.All(panels, panel =>
        {
            Assert.False(string.IsNullOrWhiteSpace(panel.GetProperty("title").GetString()), "a panel has no title");
            Assert.True(panel.GetProperty("id").GetInt32() > 0, "a panel has no id");
            Assert.True(panel.TryGetProperty("gridPos", out _), "a panel has no place on the grid");
        });
        Assert.Equal(panels.Count, panels.Select(panel => panel.GetProperty("id").GetInt32()).Distinct().Count());
    }

    [Fact]
    public void EveryPanelSaysWhatItShows()
    {
        var undescribed = _dashboard.RootElement.GetProperty("panels").EnumerateArray()
            .Where(panel => panel.GetProperty("type").GetString() != "row")
            .Where(panel => !panel.TryGetProperty("description", out var text) || string.IsNullOrWhiteSpace(text.GetString()))
            .Select(panel => panel.GetProperty("title").GetString())
            .ToList();

        Assert.Empty(undescribed);
    }

    private static IEnumerable<string> Expressions() =>
        _dashboard.RootElement.GetProperty("panels").EnumerateArray()
            .Where(panel => panel.TryGetProperty("targets", out _))
            .SelectMany(panel => panel.GetProperty("targets").EnumerateArray())
            .Select(target => target.GetProperty("expr").GetString() ?? "");

    private static string EverythingMeasured()
    {
        var test = new Speedtest
        {
            Id = 1, ServerId = 12345, ServerName = "Acme Fibre", ServerHost = "speed.acme.example",
            Ping = 12, Jitter = 0.4, Download = 941.25, Upload = 110.5, Time = 14,
            Status = TestStatus.Completed, Healthy = true, PacketLoss = 0.5,
            Bufferbloat = 18.5, LatencyIdle = 13.2, LatencyLoaded = 31.7, LatencyLoadedTail = 64.2,
            DownloadBytes = 903347628, UploadBytes = 88429797,
            ThresholdPing = 25, ThresholdDownload = 900, ThresholdUpload = 100,
            Created = new DateTime(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc)
        };
        var uptime = new UptimeDto(99.9, 86400, 86, 1);
        var monitoring = new MonitoringStatusDto(
            ConnectionHealth.Up, Watching: true, Since: test.Created, LastRoundAt: test.Created, FastestMilliseconds: 12.4,
            CurrentOutage: null, uptime, uptime, uptime);

        return PrometheusMetrics.Format(new ResultsSummary(test, test, 1), new DataUsedDto(1, 2, 3, 4), monitoring);
    }

    private static string Beside(string path, [CallerFilePath] string testFile = "") =>
        Path.Combine(Path.GetDirectoryName(testFile)!, path);

    [GeneratedRegex("speedtest_watcher_[a-z_]+")]
    private static partial Regex MetricName();
}
