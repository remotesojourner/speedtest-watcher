using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Application.Events;
using SpeedtestWatcher.Application.Info;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Application.Security;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IAppEvents, AppEvents>();
        services.AddSingleton<RunState>();

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
        services.AddScoped<SystemInfoService>();
        return services;
    }
}
