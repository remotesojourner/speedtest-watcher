using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.TestSupport;

namespace SpeedtestWatcher.IntegrationTests.Fixtures;

internal static class RunTestServices
{
    public static async Task<ServiceProvider> BuildAsync(
        TestDatabase database,
        ISpeedtestRunner runner,
        CancellationToken cancellationToken,
        IIntegrationDispatcher? dispatcher = null,
        ICurrentAccess? access = null,
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddApplication();
        services.AddDbContext<SpeedtestWatcherDbContext>(database.Configure);
        services.AddScoped<ISettingsStore, SettingsStore>();
        services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IConnectivityChecker, ConnectivityChecker>();
        services.AddSingleton<IServerListProvider>(new NoServerLists());
        services.AddSingleton<IConnectionProbe>(new OfflineProbe());
        services.AddSingleton<INetworkTraffic>(new ScriptedTraffic());
        services.AddSingleton(TestSampling.Bufferbloat);
        services.AddSingleton(runner);
        services.AddSingleton(access ?? FixedAccess.Full);
        services.AddSingleton(A.Fake<IHostApplicationLifetime>());
        services.AddSingleton(A.Fake<ISignInState>());
        if (dispatcher != null) services.AddSingleton(dispatcher);
        else services.AddIntegrations();
        configure?.Invoke(services);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsStore>();
        await settings.InsertDefaultsAsync(cancellationToken);
        await settings.SaveAsync(new Dictionary<string, string> { ["provider"] = "ookla", ["internetCheckEnabled"] = "false" }, cancellationToken);
        return provider;
    }

    private sealed class NoServerLists : IServerListProvider
    {
        public Task<IReadOnlyList<ServerInfo>?> GetServersAsync(SpeedtestProvider provider, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ServerInfo>?>([]);
    }
}
