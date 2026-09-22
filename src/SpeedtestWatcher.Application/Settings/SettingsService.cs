using SpeedtestWatcher.Application.Events;
using SpeedtestWatcher.Application.Security;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Events;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Settings;

namespace SpeedtestWatcher.Application.Settings;

public sealed class SettingsService
{
    private readonly ISettingsStore _store;
    private readonly IIntegrationDispatcher _dispatcher;
    private readonly IAppEvents _events;
    private readonly ISignInState _signIn;
    private readonly ICurrentAccess _access;

    public SettingsService(ISettingsStore store, IIntegrationDispatcher dispatcher, IAppEvents events, ISignInState signIn, ICurrentAccess access)
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
