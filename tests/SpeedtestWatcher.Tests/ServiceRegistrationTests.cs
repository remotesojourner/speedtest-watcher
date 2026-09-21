using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Infrastructure.Data;
using SpeedtestWatcher.Infrastructure.Repositories;
using SpeedtestWatcher.Web.Services;

namespace SpeedtestWatcher.Tests;

public class ServiceRegistrationTests
{
    private static IServiceCollection CreateBaseServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(A.Fake<IJSRuntime>());
        services.AddSingleton<NavigationManager>(new TestNavigationManager());
        return services;
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("http://localhost:2003/", "http://localhost:2003/");
    }

    [Fact]
    public void MudServices_ResolveSuccessfully()
    {
        var services = CreateBaseServices();
        services.AddMudServices();

        var sp = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = sp.CreateScope();
        var dialogService = scope.ServiceProvider.GetService<IDialogService>();
        var snackbar = scope.ServiceProvider.GetService<ISnackbar>();

        Assert.NotNull(dialogService);
        Assert.NotNull(snackbar);
    }

    [Fact]
    public void AppStateServices_ResolveInScope()
    {
        var services = CreateBaseServices();
        services.AddHttpClient();
        services.AddDbContext<SpeedtestWatcherDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        services.AddScoped<ISettingsStore, SettingsStore>();
        services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();

        services.AddMudServices();
        services.AddScoped<ApiClient>();
        services.AddScoped<BrowserInterop>();
        services.AddScoped<PreferencesService>();
        services.AddScoped<StatusStateService>();
        services.AddScoped<SpeedtestStateService>();
        services.AddScoped<ConfigStateService>();
        services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://localhost:2003/") });

        var sp = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = sp.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<PreferencesService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<StatusStateService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ConfigStateService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDialogService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISnackbar>());
    }
}
