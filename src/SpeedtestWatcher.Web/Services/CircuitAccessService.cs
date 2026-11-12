using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Web.Utils;

namespace SpeedtestWatcher.Web.Services;

public sealed class CircuitAccessService : ICurrentAccessService, IDisposable
{
    private readonly AuthSettingsService _auth;
    private readonly AuthenticationStateProvider _authState;
    private readonly ClaimsPrincipal _anonymousUser = new(new ClaimsIdentity());
    private Task<AuthenticationState>? _state;

    public CircuitAccessService(AuthSettingsService auth, AuthenticationStateProvider authState)
    {
        _auth = auth;
        _authState = authState;
        _authState.AuthenticationStateChanged += UpdateAuthenticationState;
    }

    public bool IsStarted { get; private set; }

    public ClaimsPrincipal User
    {
        get
        {
            var state = Volatile.Read(ref _state);
            return state is { IsCompletedSuccessfully: true } ? state.Result.User : _anonymousUser;
        }
    }

    public bool SignedIn => User.Identity?.IsAuthenticated == true;

    public Access Level => IsStarted ? AccessPolicy.Decide(_auth.Current, SignedIn, validApiToken: false) : Access.None;

    public async Task StartAsync()
    {
        var initialState = _authState.GetAuthenticationStateAsync();
        var state = Interlocked.CompareExchange(ref _state, initialState, null) ?? initialState;
        await state;
        IsStarted = true;
    }

    public void Dispose() => _authState.AuthenticationStateChanged -= UpdateAuthenticationState;

    private void UpdateAuthenticationState(Task<AuthenticationState> state) => Volatile.Write(ref _state, state);
}
