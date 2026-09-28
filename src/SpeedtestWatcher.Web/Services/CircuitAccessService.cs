using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Web.Utils;

namespace SpeedtestWatcher.Web.Services;

public sealed class CircuitAccessService : ICurrentAccessService
{
    private readonly AuthSettingsService _auth;
    private readonly AuthenticationStateProvider _authState;

    public CircuitAccessService(AuthSettingsService auth, AuthenticationStateProvider authState)
    {
        _auth = auth;
        _authState = authState;
    }

    public bool IsStarted { get; private set; }

    public ClaimsPrincipal User { get; private set; } = new(new ClaimsIdentity());

    public bool SignedIn => User.Identity?.IsAuthenticated == true;

    public Access Level => IsStarted ? AccessPolicy.Decide(_auth.Current, SignedIn, validApiToken: false) : Access.None;

    public async Task StartAsync()
    {
        User = (await _authState.GetAuthenticationStateAsync()).User;
        IsStarted = true;
    }
}
