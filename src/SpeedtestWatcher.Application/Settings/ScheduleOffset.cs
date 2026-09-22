namespace SpeedtestWatcher.Application.Settings;

public static class ScheduleOffset
{
    public static TimeSpan Shortest { get; } = TimeSpan.FromSeconds(30);

    public static TimeSpan Longest { get; } = TimeSpan.FromMinutes(5);

    public static TimeSpan LongestWithin(TimeSpan gapBetweenRuns) =>
        gapBetweenRuns / 4 < Longest ? gapBetweenRuns / 4 : Longest;

    public static TimeSpan? Within(TimeSpan gapBetweenRuns)
    {
        var longest = LongestWithin(gapBetweenRuns);
        return longest < Shortest
            ? null
            : TimeSpan.FromSeconds(Random.Shared.Next((int)Shortest.TotalSeconds, (int)longest.TotalSeconds + 1));
    }
}
