using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Settings;

namespace SpeedtestWatcher.Tests.Browser;

public abstract class BrowserApp : TestApp
{
    protected BrowserApp()
    {
        UseKestrel(0);
    }

    public HeldSpeedtestRunner Runner { get; } = new();

    public Uri BaseAddress => new(Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.First());

    public async Task<AppSettings> SettingsAsync(CancellationToken cancellationToken)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISettingsStore>().GetAsync(cancellationToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISpeedtestRunner>();
            services.AddSingleton<ISpeedtestRunner>(Runner);
            services.RemoveAll<IConnectivityChecker>();
            services.AddSingleton<IConnectivityChecker, AlwaysConnected>();
            services.RemoveAll<IServerListProvider>();
            services.AddSingleton<IServerListProvider, FixedServerList>();
        });
    }
}
