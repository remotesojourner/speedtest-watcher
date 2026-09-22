using Cronos;

namespace SpeedtestWatcher.Application.Settings;

public sealed record ScheduleSettings(string Cron, bool RandomOffset)
{
    private const int DaysInAMonth = 30;
    private const int MostRunsCounted = 50_000;

    public DateTime? NextRunAfter(DateTime utc) =>
        CronExpression.Parse(Cron, CronFormat.Standard).GetNextOccurrence(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.Utc);

    public int RunsPerMonth(DateTime utc)
    {
        var schedule = CronExpression.Parse(Cron, CronFormat.Standard);
        var until = DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddDays(DaysInAMonth);
        var at = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var runs = 0;

        while (runs < MostRunsCounted && schedule.GetNextOccurrence(at, TimeZoneInfo.Utc) is { } next && next <= until)
        {
            runs++;
            at = next;
        }

        return runs;
    }
}
