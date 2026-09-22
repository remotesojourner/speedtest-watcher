using FakeItEasy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpeedtestWatcher.Application;
using SpeedtestWatcher.Application.Security;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.SpeedTest;
using SpeedtestWatcher.Infrastructure.Data;
using SpeedtestWatcher.Infrastructure.Integrations;
using SpeedtestWatcher.Infrastructure.Network;
using SpeedtestWatcher.Infrastructure.Repositories;

namespace SpeedtestWatcher.Tests;

internal static class RunTestServices
{
    public static async Task<ServiceProvider> BuildAsync(
        SqliteConnection connection,
        ISpeedtestRunner runner,
        CancellationToken cancellationToken,
        IIntegrationDispatcher? dispatcher = null,
        ICurrentAccess? access = null,
        Action<IServiceCollection>? configure = null)
    {
        connection.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddApplication();
        services.AddDbContext<SpeedtestWatcherDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<ISettingsStore, SettingsStore>();
        services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IConnectivityChecker, ConnectivityChecker>();
        services.AddSingleton<IServerListProvider>(new NoServerLists());
        services.AddSingleton(runner);
        services.AddSingleton(access ?? FixedAccess.Full);
        services.AddSingleton(A.Fake<IHostApplicationLifetime>());
        services.AddSingleton(A.Fake<ISignInState>());
        if (dispatcher != null) services.AddSingleton(dispatcher);
        else services.AddIntegrations();
        configure?.Invoke(services);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SpeedtestWatcherDbContext>().Database.EnsureCreatedAsync(cancellationToken);
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
