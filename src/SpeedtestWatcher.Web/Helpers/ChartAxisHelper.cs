namespace SpeedtestWatcher.Web.Helpers;

public static class ChartAxisHelper
{
    private const int MostPointsWithSeparateMarkers = 30;

    public static bool HasRoomForMarkers(int pointCount) => pointCount <= MostPointsWithSeparateMarkers;

    public static string[] ThinLabels(string[] labels, int max)
    {
        if (max <= 0 || labels.Length <= max) return labels;

        var gaps = Math.Max(max, 2) - 1;
        var kept = Enumerable.Range(0, gaps + 1)
            .Select(i => (int)Math.Round(i * (labels.Length - 1) / (double)gaps))
            .ToHashSet();
        return labels.Select((label, index) => kept.Contains(index) ? label : "").ToArray();
    }

    public static int TickStep(IEnumerable<double> values, bool beginAtZero = false, int targetTicks = 4)
    {
        var list = values.ToList();
        if (list.Count == 0) return 1;

        var axisBottom = beginAtZero ? Math.Min(0, list.Min()) : list.Min();
        var raw = (list.Max() - axisBottom) / targetTicks;
        if (raw <= 1) return 1;

        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var normalized = raw / magnitude;
        var nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
        return (int)(nice * magnitude);
    }
}
