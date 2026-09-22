using Cronos;

namespace SpeedtestWatcher.Application.Settings;

public sealed record ScheduleSettings(string Cron, bool RandomOffset, string? UnhealthyCron = null)
{
    private const int MostRunsCounted = 50_000;

    public bool HasUnhealthySchedule => !string.IsNullOrWhiteSpace(UnhealthyCron);

    public DateTime? NextRunAfter(DateTime utc, bool unhealthy = false) =>
        Parse(CronFor(unhealthy)).GetNextOccurrence(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.Utc);

    public int RunsIn(TimeSpan window, DateTime utc)
    {
        var schedule = Parse(Cron);
        var until = DateTime.SpecifyKind(utc, DateTimeKind.Utc).Add(window);
        var at = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var runs = 0;

        while (runs < MostRunsCounted && schedule.GetNextOccurrence(at, TimeZoneInfo.Utc) is { } next && next <= until)
        {
            runs++;
            at = next;
        }

        return runs;
    }

    private string CronFor(bool unhealthy) => unhealthy && HasUnhealthySchedule ? UnhealthyCron! : Cron;

    private static CronExpression Parse(string cron) => CronExpression.Parse(cron, CronFormat.Standard);
}
