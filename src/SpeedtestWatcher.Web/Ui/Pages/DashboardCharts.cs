using MudBlazor;
using SpeedtestWatcher.Application.Statistics;
using SpeedtestWatcher.Web.Resources;
using SpeedtestWatcher.Web.Ui.Display;

namespace SpeedtestWatcher.Web.Ui.Pages;

public sealed class DashboardCharts
{
    private DashboardCharts(
        string[] labels,
        List<ChartSeries<double>> download,
        List<ChartSeries<double>> upload,
        List<ChartSeries<double>> ping,
        string[] bufferbloatLabels,
        List<ChartSeries<double>> bufferbloat,
        (int Download, int Upload, int Ping, int Bufferbloat) tickSteps,
        bool showMarkers,
        bool showBufferbloatMarkers,
        IReadOnlyList<ChartPoint> failures)
    {
        Labels = labels;
        Download = download;
        Upload = upload;
        Ping = ping;
        BufferbloatLabels = bufferbloatLabels;
        Bufferbloat = bufferbloat;
        TickSteps = tickSteps;
        ShowMarkers = showMarkers;
        ShowBufferbloatMarkers = showBufferbloatMarkers;
        Failures = failures;
    }

    public static DashboardCharts Empty { get; } = new([], [], [], [], [], [], (0, 0, 0, 0), false, false, []);

    public string[] Labels { get; }

    public List<ChartSeries<double>> Download { get; }

    public List<ChartSeries<double>> Upload { get; }

    public List<ChartSeries<double>> Ping { get; }

    public string[] BufferbloatLabels { get; }

    public List<ChartSeries<double>> Bufferbloat { get; }

    public (int Download, int Upload, int Ping, int Bufferbloat) TickSteps { get; }

    public bool ShowMarkers { get; }

    public bool ShowBufferbloatMarkers { get; }

    public bool HasBufferbloat => BufferbloatLabels.Length > 0;

    public IReadOnlyList<ChartPoint> Failures { get; }

    public static DashboardCharts Build(IReadOnlyList<ChartPoint> points, Func<double, double> convertSpeed, Func<DateTime, string> label, bool beginAtZero)
    {
        var readings = points.Where(point => !point.Failed && point.Download != null).ToList();

        var download = readings.Select(point => convertSpeed(point.Download!.Value)).ToArray();
        var upload = readings.Select(point => convertSpeed(point.Upload ?? 0)).ToArray();
        var ping = readings.Select(point => (double)(point.Ping ?? 0)).ToArray();
        var jitter = readings.Select(point => point.Jitter ?? 0).ToArray();

        var bloated = readings.Where(point => point.BufferbloatDown.HasValue || point.BufferbloatUp.HasValue).ToList();
        var bufferbloatDown = bloated.Select(point => point.BufferbloatDown ?? 0).ToArray();
        var bufferbloatUp = bloated.Select(point => point.BufferbloatUp ?? 0).ToArray();

        return new DashboardCharts(
            readings.Select(point => label(point.Time)).ToArray(),
            [new ChartSeries<double> { Name = WebStrings.Download, Data = download }, Average(download)],
            [new ChartSeries<double> { Name = WebStrings.Upload, Data = upload }, Average(upload)],
            [new ChartSeries<double> { Name = WebStrings.Ping, Data = ping }, new ChartSeries<double> { Name = WebStrings.Jitter, Data = jitter }, Average(ping)],
            bloated.Select(point => label(point.Time)).ToArray(),
            bloated.Count == 0
                ? []
                : [new ChartSeries<double> { Name = WebStrings.Download, Data = bufferbloatDown }, new ChartSeries<double> { Name = WebStrings.Upload, Data = bufferbloatUp }],
            (ChartAxisHelper.TickStep(download, beginAtZero),
                ChartAxisHelper.TickStep(upload, beginAtZero),
                ChartAxisHelper.TickStep(ping.Concat(jitter), beginAtZero),
                ChartAxisHelper.TickStep(bufferbloatDown.Concat(bufferbloatUp), beginAtZero)),
            ChartAxisHelper.HasRoomForMarkers(readings.Count),
            ChartAxisHelper.HasRoomForMarkers(bloated.Count),
            points.Where(point => point.Failed).Reverse().ToList());
    }

    private static ChartSeries<double> Average(double[] values) => new()
    {
        Name = WebStrings.Average,
        Data = values.Length == 0 ? [] : Enumerable.Repeat(Math.Round(values.Average(), 2), values.Length).ToArray()
    };
}
