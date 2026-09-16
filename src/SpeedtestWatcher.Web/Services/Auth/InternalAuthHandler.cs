using Microsoft.AspNetCore.Components.Authorization;

namespace SpeedtestWatcher.Web.Services.Auth;

public sealed class InternalAuthHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authState;
    private readonly InternalAccessToken _token;

    public InternalAuthHandler(AuthenticationStateProvider authState, InternalAccessToken token)
    {
        _authState = authState;
        _token = token;
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (await IsSignedInAsync(_authState))
            request.Headers.TryAddWithoutValidation(InternalAccessToken.HeaderName, _token.Value);

        return await base.SendAsync(request, cancellationToken);
    }

    public static async Task<bool> IsSignedInAsync(AuthenticationStateProvider authState)
    {
        try
        {
            var state = await authState.GetAuthenticationStateAsync();
            return state.User.Identity?.IsAuthenticated == true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
