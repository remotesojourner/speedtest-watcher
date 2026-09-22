using MudBlazor;
using SpeedtestWatcher.Application.Statistics;
using SpeedtestWatcher.Web.Helpers;

namespace SpeedtestWatcher.Web.Components.Pages;

public sealed class DashboardCharts
{
    private DashboardCharts(
        string[] labels,
        List<ChartSeries<double>> download,
        List<ChartSeries<double>> upload,
        List<ChartSeries<double>> ping,
        (int Download, int Upload, int Ping) tickSteps,
        bool showMarkers,
        IReadOnlyList<ChartPoint> failures)
    {
        Labels = labels;
        Download = download;
        Upload = upload;
        Ping = ping;
        TickSteps = tickSteps;
        ShowMarkers = showMarkers;
        Failures = failures;
    }

    public static DashboardCharts Empty { get; } = new([], [], [], [], (0, 0, 0), false, []);

    public string[] Labels { get; }

    public List<ChartSeries<double>> Download { get; }

    public List<ChartSeries<double>> Upload { get; }

    public List<ChartSeries<double>> Ping { get; }

    public (int Download, int Upload, int Ping) TickSteps { get; }

    public bool ShowMarkers { get; }

    public IReadOnlyList<ChartPoint> Failures { get; }

    public static DashboardCharts Build(IReadOnlyList<ChartPoint> points, Func<double, double> convertSpeed, Func<DateTime, string> label, bool beginAtZero)
    {
        var readings = points.Where(point => !point.Failed && point.Download != null).ToList();

        var download = readings.Select(point => convertSpeed(point.Download!.Value)).ToArray();
        var upload = readings.Select(point => convertSpeed(point.Upload ?? 0)).ToArray();
        var ping = readings.Select(point => (double)(point.Ping ?? 0)).ToArray();
        var jitter = readings.Select(point => point.Jitter ?? 0).ToArray();

        return new DashboardCharts(
            readings.Select(point => label(point.Time)).ToArray(),
            [new ChartSeries<double> { Name = "Download", Data = download }, Average(download)],
            [new ChartSeries<double> { Name = "Upload", Data = upload }, Average(upload)],
            [new ChartSeries<double> { Name = "Ping", Data = ping }, new ChartSeries<double> { Name = "Jitter", Data = jitter }, Average(ping)],
            (ChartAxisHelper.TickStep(download, beginAtZero), ChartAxisHelper.TickStep(upload, beginAtZero), ChartAxisHelper.TickStep(ping.Concat(jitter), beginAtZero)),
            ChartAxisHelper.HasRoomForMarkers(readings.Count),
            points.Where(point => point.Failed).Reverse().ToList());
    }

    private static ChartSeries<double> Average(double[] values) => new()
    {
        Name = "Average",
        Data = values.Length == 0 ? [] : Enumerable.Repeat(Math.Round(values.Average(), 2), values.Length).ToArray()
    };
}
