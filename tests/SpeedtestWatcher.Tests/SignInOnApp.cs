using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Tests;

public abstract class SignInOnApp : TestApp
{
    public string Token { get; } = ApiToken.Generate();

    protected abstract string VisitorAccess { get; }

    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var config = services.GetRequiredService<IConfigRepository>();
        await config.UpdateValueAsync("provider", "ookla", cancellationToken);
        await config.UpdateValueAsync("authEnabled", "true", cancellationToken);
        await config.UpdateValueAsync("oidcAuthority", "https://localhost/identity-provider", cancellationToken);
        await config.UpdateValueAsync("oidcClientId", "speedtest-watcher", cancellationToken);
        await config.UpdateValueAsync("visitorAccess", VisitorAccess, cancellationToken);
        await config.UpdateValueAsync("apiTokenHash", ApiToken.Hash(Token), cancellationToken);
    }
}
