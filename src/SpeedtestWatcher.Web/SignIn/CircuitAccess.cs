using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Web.SignIn;

public sealed class CircuitAccess : ICurrentAccess
{
    private readonly AuthSettings _auth;
    private readonly AuthenticationStateProvider _authState;

    public CircuitAccess(AuthSettings auth, AuthenticationStateProvider authState)
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
