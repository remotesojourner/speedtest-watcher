using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Resources;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Application.Monitoring;

public sealed class MonitoringService
{
    public const int MostOutagesListed = 200;
    public const int DaysInTheYearView = 365;

    public static IReadOnlyList<LatencyRange> LatencyRanges =>
    [
        new("1h", ApplicationStrings.LatencyRange1Hour, TimeSpan.FromHours(1), 1),
        new("6h", ApplicationStrings.LatencyRange6Hours, TimeSpan.FromHours(6), 2),
        new("24h", ApplicationStrings.LatencyRange24Hours, TimeSpan.FromHours(24), 5),
        new("7d", ApplicationStrings.LatencyRange7Days, TimeSpan.FromDays(7), 30),
        new("30d", ApplicationStrings.LatencyRange30Days, TimeSpan.FromDays(30), 120)
    ];

    private readonly IMonitoringRepository _monitoring;
    private readonly ISettingsStore _settings;
    private readonly ConnectionState _state;
    private readonly ICurrentAccess _access;

    public MonitoringService(IMonitoringRepository monitoring, ISettingsStore settings, ConnectionState state, ICurrentAccess access)
    {
        _monitoring = monitoring;
        _settings = settings;
        _state = state;
        _access = access;
    }

    public async Task<MonitoringStatusDto> StatusAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var snapshot = _state.Current;
        var watching = (await _settings.GetAsync(cancellationToken)).Monitoring.Watching;
        var sessions = await _monitoring.ListWatchSessionsSinceAsync(now.AddDays(-30), cancellationToken);
        var outages = await _monitoring.ListOutagesSinceAsync(now.AddDays(-30), MostOutagesListed, cancellationToken);
        var open = await _monitoring.GetOpenOutageAsync(cancellationToken);

        return new MonitoringStatusDto(
            snapshot.Health,
            watching,
            snapshot.Since,
            snapshot.LastRoundAt,
            snapshot.FastestMilliseconds,
            open is null ? null : Describe(open, now),
            Uptime.Over(now.AddHours(-24), now, sessions, outages, now),
            Uptime.Over(now.AddDays(-7), now, sessions, outages, now),
            Uptime.Over(now.AddDays(-30), now, sessions, outages, now));
    }

    public async Task<UptimeDto> UptimeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var sessions = await _monitoring.ListWatchSessionsSinceAsync(fromUtc, cancellationToken);
        var outages = await _monitoring.ListOutagesSinceAsync(fromUtc, MostOutagesListed, cancellationToken);
        return Uptime.Over(fromUtc, toUtc, sessions, outages, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<OutageDto>> OutagesAsync(int limit, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var outages = await _monitoring.ListOutagesSinceAsync(now.AddDays(-DaysInTheYearView), Math.Clamp(limit, 1, MostOutagesListed), cancellationToken);
        return [.. outages.Select(outage => Describe(outage, now))];
    }

    public async Task<IReadOnlyList<UptimeDayDto>> DaysAsync(string? timeZoneId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        TimeZones.TryFindTimeZone(timeZoneId ?? "", out var timeZone);
        var outages = await _monitoring.ListOutagesSinceAsync(now.AddDays(-DaysInTheYearView), MostOutagesListed, cancellationToken);
        return Uptime.ByDay(DaysInTheYearView, outages, timeZone, now);
    }

    public async Task<IReadOnlyList<LatencyPointDto>> LatencyAsync(string? rangeId, CancellationToken cancellationToken = default)
    {
        var range = RangeFor(rangeId);
        var now = DateTime.UtcNow;
        return await _monitoring.LatencyAsync(now - range.Window, now, range.SlotMinutes, cancellationToken);
    }

    public static LatencyRange RangeFor(string? rangeId) =>
        LatencyRanges.FirstOrDefault(range => string.Equals(range.Id, rangeId, StringComparison.OrdinalIgnoreCase)) ?? LatencyRanges[2];

    public async Task<OperationResult> DeleteOutageAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        return await _monitoring.DeleteOutageAsync(id, cancellationToken)
            ? OperationResult.Ok()
            : OperationResult.NotFound(ApplicationStrings.OutageNotFound);
    }

    private static OutageDto Describe(Outage outage, DateTime now) => new(
        outage.Id,
        outage.StartedAt,
        outage.EndedAt,
        (long)Math.Round(((outage.EndedAt ?? now) - outage.StartedAt).TotalSeconds));
}
