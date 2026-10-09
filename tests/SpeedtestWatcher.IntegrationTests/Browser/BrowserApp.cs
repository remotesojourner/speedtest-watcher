using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Repositories.Interfaces;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.IntegrationTests.Browser;

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
        return await scope.ServiceProvider.GetRequiredService<ISettingsRepository>().GetAsync(cancellationToken);
    }

    public async Task<SettingsSaveResult> SaveSettingsAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISettingsRepository>().SaveAsync(changes, cancellationToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IToolRunnerService>();
            services.AddSingleton<IToolRunnerService>(Runner);
            services.RemoveAll<IConnectivityCheckService>();
            services.AddSingleton<IConnectivityCheckService, AlwaysConnected>();
            services.RemoveAll<IServerListService>();
            services.AddSingleton<IServerListService, FixedServerList>();
        });
    }
}
