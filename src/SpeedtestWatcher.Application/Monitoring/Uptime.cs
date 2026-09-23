using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Monitoring;

public static class Uptime
{
    public static UptimeDto Over(
        DateTime fromUtc,
        DateTime toUtc,
        IReadOnlyList<WatchSession> sessions,
        IReadOnlyList<Outage> outages,
        DateTime nowUtc)
    {
        var watched = sessions.Sum(session => Overlap(session.StartedAt, session.LastSeenAt, fromUtc, toUtc));
        var down = outages.Sum(outage => Overlap(outage.StartedAt, outage.EndedAt ?? nowUtc, fromUtc, toUtc));
        var counted = outages.Count(outage => Overlap(outage.StartedAt, outage.EndedAt ?? nowUtc, fromUtc, toUtc) > 0);

        return new UptimeDto(
            watched > 0 ? Math.Round(Math.Clamp((watched - down) / watched, 0, 1) * 100, 3) : null,
            (long)Math.Round(watched),
            (long)Math.Round(Math.Min(down, watched)),
            counted);
    }

    public static IReadOnlyList<UptimeDayDto> ByDay(
        int days,
        IReadOnlyList<Outage> outages,
        TimeZoneInfo timeZone,
        DateTime nowUtc)
    {
        var today = DateOnly.FromDateTime(TimeZones.InTimeZone(nowUtc, timeZone));
        var byDay = new List<UptimeDayDto>(days);

        for (var ago = days - 1; ago >= 0; ago--)
        {
            var date = today.AddDays(-ago);
            var fromUtc = TimeZones.WallClockToUtc(date.ToDateTime(TimeOnly.MinValue), timeZone);
            var toUtc = TimeZones.WallClockToUtc(date.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);
            var onThisDay = outages
                .Select(outage => Overlap(outage.StartedAt, outage.EndedAt ?? nowUtc, fromUtc, toUtc))
                .Where(seconds => seconds > 0)
                .ToList();

            byDay.Add(new UptimeDayDto(date, onThisDay.Count, (long)Math.Round(onThisDay.Sum())));
        }

        return byDay;
    }

    private static double Overlap(DateTime from, DateTime to, DateTime windowFrom, DateTime windowTo)
    {
        var start = from > windowFrom ? from : windowFrom;
        var end = to < windowTo ? to : windowTo;
        return end > start ? (end - start).TotalSeconds : 0;
    }
}
