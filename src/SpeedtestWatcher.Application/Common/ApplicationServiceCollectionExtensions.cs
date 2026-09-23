using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Statistics;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.Application.Updates;

namespace SpeedtestWatcher.Application.Common;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IAppEvents, AppEvents>();
        services.AddSingleton<RunState>();
        services.AddSingleton<ConnectionState>();

        services.AddScoped<ServerSelector>();
        services.AddScoped<SpeedtestRunService>();
        services.AddScoped<ResultsService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<PauseService>();
        services.AddScoped<RecommendationService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<SettingsBackupService>();
        services.AddScoped<SignInService>();
        services.AddScoped<IntegrationService>();
        services.AddScoped<StorageService>();
        services.AddScoped<VersionService>();
        services.AddScoped<ProviderOptionsService>();
        services.AddScoped<MonitoringService>();
        services.AddScoped<BufferbloatMeter>();

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
        services.AddScoped<IMonitoringRepository, MonitoringRepository>();

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
        services.AddSingleton<IReleaseChecker, GitHubReleaseChecker>();
        services.AddSingleton<IConnectionProbe, TcpConnectionProbe>();
        services.TryAddSingleton(BufferbloatSampling.Default);

        services.AddIntegrations();

        services.AddHostedService<SpeedtestSchedulerService>();
        services.AddHostedService<ConnectivityMonitorService>();
        services.AddHostedService<RetentionCleanupService>();
        services.AddHostedService<IntegrationTickerService>();
        services.AddHostedService<InterfaceRefreshService>();
        services.AddHostedService<CliDownloadService>();
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
