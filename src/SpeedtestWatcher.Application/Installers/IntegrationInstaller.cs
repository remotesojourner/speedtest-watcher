using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Application.Services;
using SpeedtestWatcher.Application.Services.Integrations;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Application.Services.Integrations.Interfaces;

namespace SpeedtestWatcher.Application.Installers;

public static class IntegrationInstaller
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationTypeService, DiscordIntegrationService>();
        services.AddScoped<IIntegrationTypeService, TelegramIntegrationService>();
        services.AddScoped<IIntegrationTypeService, GotifyIntegrationService>();
        services.AddScoped<IIntegrationTypeService, NtfyIntegrationService>();
        services.AddScoped<IIntegrationTypeService, PushoverIntegrationService>();
        services.AddScoped<IIntegrationTypeService, AppriseIntegrationService>();
        services.AddScoped<IIntegrationTypeService, WebhookIntegrationService>();
        services.AddScoped<IIntegrationTypeService, HealthchecksIntegrationService>();
        services.AddScoped<IIntegrationTypeService, InfluxDbIntegrationService>();
        services.AddSingleton<HeartbeatScheduleService>();
        services.AddScoped<IIntegrationDispatchService, IntegrationDispatchService>();
        return services;
    }
}
