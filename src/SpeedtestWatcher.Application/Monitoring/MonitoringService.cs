using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Application.Monitoring;

public sealed class MonitoringService
{
    public const int MostOutagesListed = 200;
    public const int DaysInTheYearView = 365;

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

    public async Task<OperationResult> DeleteOutageAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        return await _monitoring.DeleteOutageAsync(id, cancellationToken)
            ? OperationResult.Ok()
            : OperationResult.NotFound("Outage not found");
    }

    private static OutageDto Describe(Outage outage, DateTime now) => new(
        outage.Id,
        outage.StartedAt,
        outage.EndedAt,
        (long)Math.Round(((outage.EndedAt ?? now) - outage.StartedAt).TotalSeconds));
}
