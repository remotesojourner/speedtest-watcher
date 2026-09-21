using Microsoft.AspNetCore.SignalR;
using SpeedtestWatcher.Core.Events;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Settings;
using SpeedtestWatcher.Web.Hubs;

namespace SpeedtestWatcher.Web.Services;

public sealed class SettingsService
{
    private readonly ISettingsStore _store;
    private readonly IIntegrationDispatcher _dispatcher;
    private readonly IHubContext<SpeedtestHub> _hubContext;

    public SettingsService(ISettingsStore store, IIntegrationDispatcher dispatcher, IHubContext<SpeedtestHub> hubContext)
    {
        _store = store;
        _dispatcher = dispatcher;
        _hubContext = hubContext;
    }

    public async Task<SettingsSaveResult> SaveAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default)
    {
        var result = await _store.SaveAsync(changes, cancellationToken);
        if (!result.Succeeded) return result;

        foreach (var (key, value) in changes)
        {
            await _dispatcher.PublishAsync(new ConfigUpdated(key, value), cancellationToken);
            await _hubContext.Clients.All.SendAsync("ConfigChanged", key, value, cancellationToken);
        }

        return result;
    }
}
