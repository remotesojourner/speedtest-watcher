namespace SpeedtestWatcher.Web.Helpers;

/// <summary>Keeps MudChart axes readable for any number of data points.</summary>
public static class ChartAxisHelper
{
    /// <summary>
    /// Keeps at most <paramref name="max"/> evenly spaced labels, always including the first and last,
    /// and blanks the rest so long date labels don't overlap. A max of 0 keeps every label.
    /// </summary>
    public static string[] ThinLabels(string[] labels, int max)
    {
        if (max <= 0 || labels.Length <= max) return labels;

        var gaps = Math.Max(max, 2) - 1;
        var kept = Enumerable.Range(0, gaps + 1)
            .Select(i => (int)Math.Round(i * (labels.Length - 1) / (double)gaps))
            .ToHashSet();
        return labels.Select((label, index) => kept.Contains(index) ? label : "").ToArray();
    }

    /// <summary>
    /// A 1-2-5 tick spacing that splits the data range into roughly <paramref name="targetTicks"/> steps.
    /// MudChart's fixed default of 20 flattens lines whose values only vary by a few units.
    /// </summary>
    public static int TickStep(IEnumerable<double> values, int targetTicks = 4)
    {
        var list = values.ToList();
        if (list.Count == 0) return 1;

        var raw = (list.Max() - list.Min()) / targetTicks;
        if (raw <= 1) return 1;

        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var normalized = raw / magnitude;
        var nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
        return (int)(nice * magnitude);
    }
}
