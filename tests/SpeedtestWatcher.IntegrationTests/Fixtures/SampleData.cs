using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.IntegrationTests.Fixtures;

internal static class SampleData
{
    public static Task ChooseOoklaAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<ISettingsStore>().SaveAsync(new Dictionary<string, string> { ["provider"] = "ookla" }, cancellationToken);

    public static async Task SeedResultsAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var results = services.GetRequiredService<ISpeedtestRepository>();
        foreach (var result in Results())
        {
            await results.CreateAsync(result, cancellationToken);
        }
    }

    public static async Task SeedRecentResultsAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var results = services.GetRequiredService<ISpeedtestRepository>();
        var now = DateTime.UtcNow;
        for (var hoursAgo = 30; hoursAgo >= 2; hoursAgo -= 4)
        {
            await results.CreateAsync(RecentResult(now.AddHours(-hoursAgo), download: 900 + hoursAgo), cancellationToken);
        }

        await results.CreateAsync(new Speedtest
        {
            ServerId = 12345, ServerName = "Acme Fibre", ServerHost = "speed.acme.example",
            Ping = -1, Download = -1, Upload = -1,
            Status = TestStatus.Failed, ThresholdPing = 25, ThresholdDownload = 900, ThresholdUpload = 100,
            Type = TestType.Auto, Error = "Network unreachable", Created = now.AddHours(-1)
        }, cancellationToken);
    }

    public static Task TurnSignInOnAsync(IServiceProvider services, VisitorAccess visitorAccess, string apiToken, CancellationToken cancellationToken) =>
        services.GetRequiredService<ISettingsStore>().SaveSignInAsync(new Dictionary<string, string>
        {
            ["authEnabled"] = "true",
            ["oidcAuthority"] = "https://localhost/identity-provider",
            ["oidcClientId"] = "speedtest-watcher",
            ["visitorAccess"] = visitorAccess.ToName(),
            ["apiTokenHash"] = ApiToken.Hash(apiToken)
        }, cancellationToken);

    private static Speedtest RecentResult(DateTime created, double download) => new()
    {
        ServerId = 12345, ServerName = "Acme Fibre", ServerHost = "speed.acme.example",
        Ping = 12, Jitter = 0.4, Download = download, Upload = 110.5, Time = 14,
        Status = TestStatus.Completed, Healthy = true, ThresholdPing = 25, ThresholdDownload = 900, ThresholdUpload = 100,
        Type = TestType.Auto, Created = created
    };

    private static List<Speedtest> Results() =>
    [
        new()
        {
            ServerId = 12345, ServerName = "Acme Fibre", ServerHost = "speed.acme.example",
            Ping = 12, Jitter = 0.4, Download = 941.25, Upload = 110.5, Time = 14,
            Status = TestStatus.Completed, Healthy = true, ThresholdPing = 25, ThresholdDownload = 900, ThresholdUpload = 100,
            Type = TestType.Auto, ResultId = "r-1", PacketLoss = 0, DownloadBytes = 903347628, UploadBytes = 88429797,
            PublicIp = "203.0.113.9", Created = new DateTime(2026, 9, 14, 8, 5, 0, DateTimeKind.Utc)
        },
        new()
        {
            ServerId = 12345, ServerName = "Acme Fibre", ServerHost = "speed.acme.example",
            Ping = 31, Jitter = 2.75, Download = 612.5, Upload = 98.125, Time = 15,
            Status = TestStatus.Completed, Healthy = false, ThresholdPing = 25, ThresholdDownload = 900, ThresholdUpload = 100,
            Type = TestType.Custom, ResultId = "r-2", Created = new DateTime(2026, 9, 14, 20, 35, 0, DateTimeKind.Utc)
        },
        new()
        {
            Ping = -1, Download = -1, Upload = -1,
            Status = TestStatus.Skipped, Type = TestType.Auto, Error = "Public IP 203.0.113.9 is on the skip list",
            Created = new DateTime(2026, 9, 15, 6, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            ServerId = 12345, ServerName = "Acme Fibre", ServerHost = "speed.acme.example",
            Ping = -1, Download = -1, Upload = -1,
            Status = TestStatus.Failed, ThresholdPing = 25, ThresholdDownload = 900, ThresholdUpload = 100,
            Type = TestType.Auto, Error = "Network unreachable", Created = new DateTime(2026, 9, 15, 7, 0, 0, DateTimeKind.Utc)
        }
    ];
}
