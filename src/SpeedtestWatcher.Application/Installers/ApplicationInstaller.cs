using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Application.BackgroundServices;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Data;
using SpeedtestWatcher.Application.Repositories;
using SpeedtestWatcher.Application.Repositories.Interfaces;
using SpeedtestWatcher.Application.Services;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Application.Services.Providers;
using SpeedtestWatcher.Application.Services.Providers.Interfaces;

namespace SpeedtestWatcher.Application.Installers;

public static class ApplicationInstaller
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IAppEventService, AppEventService>();
        services.AddSingleton<RunStateService>();
        services.AddSingleton<ConnectionStateService>();

        services.AddScoped<ServerSelectionService>();
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
        services.AddScoped<DataBackupService>();
        services.AddScoped<VersionService>();
        services.AddScoped<ProviderOptionsService>();
        services.AddScoped<MonitoringService>();
        services.AddScoped<BufferbloatService>();

        services.AddDbContext<SpeedtestWatcherDbContext>((provider, options) =>
        {
            var databasePath = provider.GetRequiredService<IOptions<SpeedtestWatcherOptions>>().Value.DatabasePath;
            options.UseSqlite($"Data Source={databasePath};Cache=Shared");
        });

        services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IStorageRepository, StorageRepository>();
        services.AddScoped<IMonitoringRepository, MonitoringRepository>();
        services.AddScoped<IDataBackupRepository, DataBackupRepository>();

        services.AddHttpClient();
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<ISpeedtestProviderService, OoklaService>();
        services.AddSingleton<ISpeedtestProviderService, LibreSpeedService>();
        services.AddSingleton<ISpeedtestProviderService, CloudflareService>();
        services.AddSingleton<ISpeedtestProviderService, Iperf3Service>();
        services.AddSingleton<ICliBinaryService, CliBinaryService>();
        services.AddSingleton<ICliProcessService, CliProcessService>();
        services.AddScoped<IToolRunnerService, ToolRunnerService>();

        services.AddSingleton<INetworkInterfaceService, NetworkInterfaceService>();
        services.AddSingleton<IServerListService, ServerListService>();
        services.AddScoped<IConnectivityCheckService, ConnectivityCheckService>();
        services.AddScoped<IOidcDiscoveryService, OidcDiscoveryService>();
        services.AddSingleton<IReleaseService, GitHubReleaseService>();
        services.AddSingleton<IConnectionProbeService, TcpConnectionProbeService>();
        services.AddSingleton<INetworkTrafficService, NetworkTrafficService>();
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
        await scope.ServiceProvider.GetRequiredService<ISettingsRepository>().InsertDefaultsAsync(cancellationToken);
    }
}
