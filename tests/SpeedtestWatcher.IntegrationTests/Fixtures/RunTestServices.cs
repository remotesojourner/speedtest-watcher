using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpeedtestWatcher.TestSupport;
using SpeedtestWatcher.Application.Data;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Installers;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Repositories;
using SpeedtestWatcher.Application.Repositories.Interfaces;
using SpeedtestWatcher.Application.Services;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.IntegrationTests.Fixtures;

internal static class RunTestServices
{
    public static async Task<ServiceProvider> BuildAsync(
        TestDatabase database,
        IToolRunnerService runner,
        CancellationToken cancellationToken,
        IIntegrationDispatchService? dispatcher = null,
        ICurrentAccessService? access = null,
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddApplication();
        services.AddDbContext<SpeedtestWatcherDbContext>(database.Configure);
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IConnectivityCheckService, ConnectivityCheckService>();
        services.AddSingleton<IServerListService>(new NoServerLists());
        services.AddSingleton<IConnectionProbeService>(new OfflineProbe());
        services.AddSingleton<INetworkTrafficService>(new ScriptedTraffic());
        services.AddSingleton(TestSampling.Bufferbloat);
        services.AddSingleton(runner);
        services.AddSingleton(access ?? FixedAccess.Full);
        services.AddSingleton(A.Fake<IHostApplicationLifetime>());
        services.AddSingleton(A.Fake<ISignInStateService>());
        if (dispatcher != null) services.AddSingleton(dispatcher);
        else services.AddIntegrations();
        configure?.Invoke(services);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        await settings.InsertDefaultsAsync(cancellationToken);
        await settings.SaveAsync(new Dictionary<string, string> { ["provider"] = "ookla", ["internetCheckEnabled"] = "false" }, cancellationToken);
        return provider;
    }

    private sealed class NoServerLists : IServerListService
    {
        public Task<IReadOnlyList<ServerInfo>?> GetServersAsync(SpeedtestProvider provider, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ServerInfo>?>([]);
    }
}
