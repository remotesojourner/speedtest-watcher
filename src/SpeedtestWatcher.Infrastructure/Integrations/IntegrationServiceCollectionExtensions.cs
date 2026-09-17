using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Core.Integrations;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Infrastructure.Integrations;

public static class IntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddScoped<IIntegration, DiscordIntegration>();
        services.AddScoped<IIntegration, TelegramIntegration>();
        services.AddScoped<IIntegration, GotifyIntegration>();
        services.AddScoped<IIntegration, NtfyIntegration>();
        services.AddScoped<IIntegration, PushoverIntegration>();
        services.AddScoped<IIntegration, AppriseIntegration>();
        services.AddScoped<IIntegration, WebhookIntegration>();
        services.AddScoped<IIntegration, HealthchecksIntegration>();
        services.AddScoped<IIntegration, InfluxDbIntegration>();
        services.AddSingleton<HeartbeatSchedule>();
        services.AddScoped<IIntegrationDispatcher, IntegrationDispatcher>();
        return services;
    }
}
