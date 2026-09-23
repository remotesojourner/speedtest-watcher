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
        (int Download, int Upload, int Ping) tickSteps,
        bool showMarkers,
        bool hasBufferbloat,
        IReadOnlyList<ChartPoint> failures)
    {
        Labels = labels;
        Download = download;
        Upload = upload;
        Ping = ping;
        TickSteps = tickSteps;
        ShowMarkers = showMarkers;
        HasBufferbloat = hasBufferbloat;
        Failures = failures;
    }

    public static DashboardCharts Empty { get; } = new([], [], [], [], (0, 0, 0), false, false, []);

    public string[] Labels { get; }

    public List<ChartSeries<double>> Download { get; }

    public List<ChartSeries<double>> Upload { get; }

    public List<ChartSeries<double>> Ping { get; }

    public (int Download, int Upload, int Ping) TickSteps { get; }

    public bool ShowMarkers { get; }

    public bool HasBufferbloat { get; }

    public IReadOnlyList<ChartPoint> Failures { get; }

    public static DashboardCharts Build(IReadOnlyList<ChartPoint> points, Func<double, double> convertSpeed, Func<DateTime, string> label, bool beginAtZero)
    {
        var readings = points.Where(point => !point.Failed && point.Download != null).ToList();

        var download = readings.Select(point => convertSpeed(point.Download!.Value)).ToArray();
        var upload = readings.Select(point => convertSpeed(point.Upload ?? 0)).ToArray();
        var ping = readings.Select(point => (double)(point.Ping ?? 0)).ToArray();
        var jitter = readings.Select(point => point.Jitter ?? 0).ToArray();
        var hasBufferbloat = readings.Any(point => point.Bufferbloat.HasValue);
        var bufferbloat = readings.Select(point => point.Bufferbloat ?? 0).ToArray();

        List<ChartSeries<double>> latency = [new ChartSeries<double> { Name = WebStrings.Ping, Data = ping }, new ChartSeries<double> { Name = WebStrings.Jitter, Data = jitter }];
        if (hasBufferbloat) latency.Add(new ChartSeries<double> { Name = WebStrings.Bufferbloat, Data = bufferbloat });
        latency.Add(Average(ping));

        return new DashboardCharts(
            readings.Select(point => label(point.Time)).ToArray(),
            [new ChartSeries<double> { Name = WebStrings.Download, Data = download }, Average(download)],
            [new ChartSeries<double> { Name = WebStrings.Upload, Data = upload }, Average(upload)],
            latency,
            (ChartAxisHelper.TickStep(download, beginAtZero), ChartAxisHelper.TickStep(upload, beginAtZero), ChartAxisHelper.TickStep(ping.Concat(jitter).Concat(hasBufferbloat ? bufferbloat : []), beginAtZero)),
            ChartAxisHelper.HasRoomForMarkers(readings.Count),
            hasBufferbloat,
            points.Where(point => point.Failed).Reverse().ToList());
    }

    private static ChartSeries<double> Average(double[] values) => new()
    {
        Name = WebStrings.Average,
        Data = values.Length == 0 ? [] : Enumerable.Repeat(Math.Round(values.Average(), 2), values.Length).ToArray()
    };
}
