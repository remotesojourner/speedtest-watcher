using Microsoft.AspNetCore.SignalR;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Web.Hubs;

namespace SpeedtestWatcher.Web.Background;

public sealed class RunStatusBroadcaster : IHostedService
{
    private readonly IPauseStateService _status;
    private readonly IHubContext<SpeedtestHub> _hubContext;

    public RunStatusBroadcaster(IPauseStateService status, IHubContext<SpeedtestHub> hubContext)
    {
        _status = status;
        _hubContext = hubContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _status.OnStatusChanged += Broadcast;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _status.OnStatusChanged -= Broadcast;
        return Task.CompletedTask;
    }

    private void Broadcast() =>
        _ = _hubContext.Clients.All.SendAsync("StatusChanged", _status.IsRunning, _status.IsPaused);
}
