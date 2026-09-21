using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Tests;

public sealed class SignInOffApp : TestApp
{
    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await services.GetRequiredService<ISettingsStore>().SaveAsync(new Dictionary<string, string> { ["provider"] = "ookla" }, cancellationToken);

        var results = services.GetRequiredService<ISpeedtestRepository>();
        foreach (var result in SampleResults())
        {
            await results.CreateAsync(result, cancellationToken);
        }
    }

    private static List<Speedtest> SampleResults() =>
    [
        new()
        {
            ServerId = 12345, ServerName = "Acme Fibre", ServerHost = "speed.acme.example",
            Ping = 12, Jitter = 0.4, Download = 941.25, Upload = 110.5, Time = 14,
            Status = TestStatus.Completed, Healthy = true, ThresholdPing = 25, ThresholdDownload = 900, ThresholdUpload = 100,
            Type = TestType.Auto, ResultId = "r-1", Created = new DateTime(2026, 9, 14, 8, 5, 0, DateTimeKind.Utc)
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
