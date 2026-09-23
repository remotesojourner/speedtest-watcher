using FakeItEasy;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.TestSupport;

namespace SpeedtestWatcher.IntegrationTests.Application.Monitoring;

public sealed class UptimeOverPeriodTests : IDisposable
{
    private static readonly DateTime _start = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    [Fact]
    public async Task UptimeCoversOnlyThePeriodAsked()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var db = _database.NewContext())
        {
            var monitoring = new MonitoringRepository(db);
            var session = await monitoring.StartWatchingAsync(_start, cancellationToken);
            await monitoring.KeepWatchingAsync(session.Id, _start.AddDays(10), cancellationToken);
            await monitoring.StartOutageAsync(_start.AddDays(1), cancellationToken);
            await monitoring.EndOpenOutageAsync(_start.AddDays(1).AddHours(12), cancellationToken);
        }

        var withTheOutage = await UptimeAsync(_start, _start.AddDays(2), cancellationToken);
        var afterIt = await UptimeAsync(_start.AddDays(5), _start.AddDays(7), cancellationToken);

        Assert.Equal((75d, 1), (withTheOutage.Percent, withTheOutage.Outages));
        Assert.Equal((100d, 0), (afterIt.Percent, afterIt.Outages));
    }

    [Fact]
    public async Task APeriodNobodyWatchedHasNoUptime()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var uptime = await UptimeAsync(_start, _start.AddDays(7), cancellationToken);

        Assert.Null(uptime.Percent);
    }

    private async Task<UptimeDto> UptimeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        await using var db = _database.NewContext();
        var service = new MonitoringService(new MonitoringRepository(db), A.Fake<ISettingsStore>(), new ConnectionState(A.Fake<IAppEvents>()), FixedAccess.Full);
        return await service.UptimeAsync(fromUtc, toUtc, cancellationToken);
    }

    public void Dispose() => _database.Dispose();
}
