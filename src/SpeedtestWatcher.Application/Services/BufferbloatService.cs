using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Repositories.Interfaces;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.Services;

public sealed partial class BufferbloatService
{
    private readonly IConnectionProbeService _probe;
    private readonly INetworkTrafficService _traffic;
    private readonly ISettingsRepository _settings;
    private readonly TimeProvider _time;
    private readonly BufferbloatSampling _sampling;
    private readonly ILogger<BufferbloatService> _logger;

    public BufferbloatService(
        IConnectionProbeService probe,
        INetworkTrafficService traffic,
        ISettingsRepository settings,
        TimeProvider time,
        BufferbloatSampling sampling,
        ILogger<BufferbloatService> logger)
    {
        _probe = probe;
        _traffic = traffic;
        _settings = settings;
        _time = time;
        _sampling = sampling;
        _logger = logger;
    }

    public async Task<BufferbloatMeasurement> StartAsync(CancellationToken cancellationToken = default)
    {
        var targets = (await _settings.GetAsync(cancellationToken)).Monitoring.Targets;
        var measurement = new BufferbloatMeasurement(_probe, _traffic, targets, _sampling, _time, LogSamplingFailed);
        await measurement.StartAsync(cancellationToken);
        return measurement;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sampling latency around a speedtest failed, so it has no bufferbloat figure")]
    private partial void LogSamplingFailed(Exception exception);
}
