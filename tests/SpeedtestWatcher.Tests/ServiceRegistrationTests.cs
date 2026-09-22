using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using SpeedtestWatcher.Application.Info;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Application.Security;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.Web.Services;

namespace SpeedtestWatcher.Tests;

public sealed class ServiceRegistrationTests : IClassFixture<SignInOffApp>
{
    private readonly SignInOffApp _app;

    public ServiceRegistrationTests(SignInOffApp app)
    {
        _app = app;
    }

    public static TheoryData<Type> ScopedServices =>
    [
        typeof(SpeedtestRunService),
        typeof(ResultsService),
        typeof(StatisticsService),
        typeof(PauseService),
        typeof(RecommendationService),
        typeof(SettingsService),
        typeof(SettingsBackupService),
        typeof(SignInService),
        typeof(IntegrationService),
        typeof(StorageService),
        typeof(SystemInfoService),
        typeof(ICurrentAccess),
        typeof(PreferencesService),
        typeof(StatusStateService),
        typeof(SpeedtestStateService),
        typeof(ConfigStateService),
        typeof(IDialogService)
    ];

    [Theory]
    [MemberData(nameof(ScopedServices))]
    public void TheAppsCompositionResolvesEveryService(Type serviceType)
    {
        using var scope = _app.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService(serviceType));
    }
}
