using Cronos;

namespace SpeedtestWatcher.Core.Settings;

public sealed record ScheduleSettings(string Cron, bool RandomOffset)
{
    public DateTime? NextRunAfter(DateTime utc) =>
        CronExpression.Parse(Cron, CronFormat.Standard).GetNextOccurrence(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.Utc);
}
