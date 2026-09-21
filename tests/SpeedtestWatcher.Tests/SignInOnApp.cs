using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Helpers;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Tests;

public abstract class SignInOnApp : TestApp
{
    public string Token { get; } = ApiToken.Generate();

    protected abstract VisitorAccess VisitorAccess { get; }

    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var settings = services.GetRequiredService<ISettingsStore>();
        await settings.SaveAsync(new Dictionary<string, string> { ["provider"] = "ookla" }, cancellationToken);
        await settings.SaveSignInAsync(new Dictionary<string, string>
        {
            ["authEnabled"] = "true",
            ["oidcAuthority"] = "https://localhost/identity-provider",
            ["oidcClientId"] = "speedtest-watcher",
            ["visitorAccess"] = VisitorAccess.ToName(),
            ["apiTokenHash"] = ApiToken.Hash(Token)
        }, cancellationToken);
    }
}
