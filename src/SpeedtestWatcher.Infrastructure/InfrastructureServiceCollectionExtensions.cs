using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Core.Hosting;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.SpeedTest;
using SpeedtestWatcher.Infrastructure.Data;
using SpeedtestWatcher.Infrastructure.Integrations;
using SpeedtestWatcher.Infrastructure.Network;
using SpeedtestWatcher.Infrastructure.Repositories;
using SpeedtestWatcher.Infrastructure.SpeedTest;

namespace SpeedtestWatcher.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<SpeedtestWatcherDbContext>((provider, options) =>
        {
            var databasePath = provider.GetRequiredService<IOptions<SpeedtestWatcherOptions>>().Value.DatabasePath;
            options.UseSqlite($"Data Source={databasePath};Cache=Shared");
        });

        services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
        services.AddScoped<ISettingsStore, SettingsStore>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IStorageRepository, StorageRepository>();

        services.AddHttpClient();
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<ISpeedtestTool, OoklaTool>();
        services.AddSingleton<ISpeedtestTool, LibreSpeedTool>();
        services.AddSingleton<ISpeedtestTool, CloudflareTool>();
        services.AddSingleton<ICliManager, CliManager>();
        services.AddScoped<ISpeedtestRunner, SpeedtestRunner>();

        services.AddSingleton<INetworkInterfaceDetector, InterfaceDetector>();
        services.AddSingleton<IServerListProvider, ServerListProvider>();
        services.AddScoped<IConnectivityChecker, ConnectivityChecker>();
        services.AddScoped<IOidcDiscovery, OidcDiscoveryChecker>();
        services.AddScoped<IReleaseChecker, GitHubReleaseChecker>();

        services.AddIntegrations();
        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SpeedtestWatcherDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken);
        await scope.ServiceProvider.GetRequiredService<ISettingsStore>().InsertDefaultsAsync(cancellationToken);
    }
}
