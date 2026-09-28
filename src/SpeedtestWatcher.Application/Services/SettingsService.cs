using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Models.Dtos;
using SpeedtestWatcher.Application.Models.Events;
using SpeedtestWatcher.Application.Repositories.Interfaces;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.Services;

public sealed class SettingsService
{
    private readonly ISettingsRepository _store;
    private readonly IIntegrationDispatchService _dispatcher;
    private readonly IAppEventService _events;
    private readonly ISignInStateService _signIn;
    private readonly ICurrentAccessService _access;

    public SettingsService(ISettingsRepository store, IIntegrationDispatchService dispatcher, IAppEventService events, ISignInStateService signIn, ICurrentAccessService access)
    {
        _store = store;
        _dispatcher = dispatcher;
        _events = events;
        _signIn = signIn;
        _access = access;
    }

    public Task<AppSettings> GetAsync(CancellationToken cancellationToken = default) => _store.GetAsync(cancellationToken);

    public async Task<ConfigDto> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var fullAccess = _access.HasFullAccess;
        var values = await _store.GetValuesAsync(cancellationToken);

        var config = new ConfigDto();
        foreach (var definition in SettingDefinitions.All.Where(definition => definition.IsVisible(fullAccess)))
            config[definition.Key] = values[definition.Key];

        config["viewMode"] = !fullAccess;
        config["authActive"] = _signIn.IsActive;
        config["authDisabledByEnv"] = _signIn.DisabledByEnvironment;
        if (fullAccess)
        {
            var signIn = (await _store.GetAsync(cancellationToken)).SignIn;
            config["oidcClientSecretSet"] = signIn.ClientSecret != null;
            config["apiTokenSet"] = signIn.ApiTokenHash != null;
        }

        return config;
    }

    public async Task<OperationResult> SaveAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        var saved = await _store.SaveAsync(changes, cancellationToken);
        if (!saved.Succeeded) return OperationResult.Invalid(saved.Error!);

        foreach (var (key, value) in changes)
            await _dispatcher.PublishAsync(new ConfigUpdated(key, value), cancellationToken);

        _events.PublishSettingsChanged(changes);
        return OperationResult.Ok();
    }
}
