using Microsoft.AspNetCore.SignalR;
using SpeedtestWatcher.Application.Events;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Web.Hubs;

namespace SpeedtestWatcher.Web.Background;

public sealed class LiveUpdateBroadcaster : IHostedService
{
    private readonly IAppEvents _events;
    private readonly IHubContext<SpeedtestHub> _hubContext;

    public LiveUpdateBroadcaster(IAppEvents events, IHubContext<SpeedtestHub> hubContext)
    {
        _events = events;
        _hubContext = hubContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _events.RunStatusChanged += OnRunStatusChanged;
        _events.TestStarted += OnTestStarted;
        _events.TestFinished += OnTestFinished;
        _events.SettingsChanged += OnSettingsChanged;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _events.RunStatusChanged -= OnRunStatusChanged;
        _events.TestStarted -= OnTestStarted;
        _events.TestFinished -= OnTestFinished;
        _events.SettingsChanged -= OnSettingsChanged;
        return Task.CompletedTask;
    }

    private void OnRunStatusChanged(RunStatus status) =>
        _ = _hubContext.Clients.All.SendAsync("StatusChanged", status.Running, status.Paused);

    private void OnTestStarted() => _ = _hubContext.Clients.All.SendAsync("TestStarted");

    private void OnTestFinished(SpeedtestDto result) =>
        _ = _hubContext.Clients.All.SendAsync(result.Status == TestStatus.Failed ? "TestFailed" : "NewTestResult", result);

    private void OnSettingsChanged(IReadOnlyDictionary<string, string> changes)
    {
        foreach (var (key, value) in changes)
            _ = _hubContext.Clients.All.SendAsync("ConfigChanged", key, value);
    }
}
