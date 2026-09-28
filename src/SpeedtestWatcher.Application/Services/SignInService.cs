using SpeedtestWatcher.Application.Resources;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Extensions;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Repositories.Interfaces;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Application.Utils;

namespace SpeedtestWatcher.Application.Services;

public sealed class SignInService
{
    private readonly ISettingsRepository _store;
    private readonly IOidcDiscoveryService _discovery;
    private readonly ISignInStateService _signIn;
    private readonly ICurrentAccessService _access;

    public SignInService(ISettingsRepository store, IOidcDiscoveryService discovery, ISignInStateService signIn, ICurrentAccessService access)
    {
        _store = store;
        _discovery = discovery;
        _signIn = signIn;
        _access = access;
    }

    public async Task<OperationResult<bool>> SaveAsync(AuthSettingsRequest request, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        var authority = Clean(request.Authority);
        if (authority != null && !WebAddress.IsHttp(authority))
            return OperationResult.Invalid(ApplicationStrings.SignInProviderUrlInvalid);

        var clientId = Clean(request.ClientId);
        if (request.Enabled)
        {
            if (authority == null || clientId == null)
                return OperationResult.Invalid(ApplicationStrings.SignInIncomplete);

            if (await _discovery.FindProblemAsync(authority, cancellationToken) is { } problem)
                return OperationResult.Invalid(problem);
        }

        var values = new Dictionary<string, string>
        {
            ["authEnabled"] = request.Enabled ? "true" : "false",
            ["visitorAccess"] = request.VisitorAccess.ToName(),
            ["oidcAuthority"] = authority ?? SettingDefinitions.Unset,
            ["oidcClientId"] = clientId ?? SettingDefinitions.Unset,
            ["oidcScopes"] = string.Join(' ', SignInSettings.ParseScopes(request.Scopes))
        };
        if (Clean(request.ClientSecret) is { } secret) values["oidcClientSecret"] = secret;
        else if (request.ClearClientSecret) values["oidcClientSecret"] = SettingDefinitions.Unset;

        var saved = await _store.SaveSignInAsync(values, cancellationToken);
        if (!saved.Succeeded) return OperationResult.Invalid(saved.Error!);

        await _signIn.ReloadAsync(cancellationToken);
        return OperationResult.Ok(_signIn.IsActive);
    }

    public async Task<OperationResult<ApiTokenResponse>> CreateTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        var token = ApiToken.Generate();
        await SaveTokenHashAsync(ApiToken.Hash(token), cancellationToken);
        return OperationResult.Ok(new ApiTokenResponse { Token = token });
    }

    public async Task<OperationResult> RevokeTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        await SaveTokenHashAsync(SettingDefinitions.Unset, cancellationToken);
        return OperationResult.Ok();
    }

    private async Task SaveTokenHashAsync(string hash, CancellationToken cancellationToken)
    {
        await _store.SaveSignInAsync(new Dictionary<string, string> { ["apiTokenHash"] = hash }, cancellationToken);
        await _signIn.ReloadAsync(cancellationToken);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() == SettingDefinitions.Unset ? null : value.Trim();
}
