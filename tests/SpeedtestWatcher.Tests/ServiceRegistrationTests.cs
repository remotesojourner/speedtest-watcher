using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Statistics;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.Application.Updates;
using SpeedtestWatcher.Web.Services;
using SpeedtestWatcher.Web.Services.Auth;

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
        typeof(VersionService),
        typeof(ProviderOptionsService),
        typeof(ICurrentAccess),
        typeof(CircuitAccess),
        typeof(HttpCurrentAccess),
        typeof(PreferencesService),
        typeof(StatusStateService),
        typeof(SettingsState),
        typeof(RecentResults),
        typeof(LiveUpdates),
        typeof(IDialogService)
    ];

    [Theory]
    [MemberData(nameof(ScopedServices))]
    public void TheAppsCompositionResolvesEveryService(Type serviceType)
    {
        using var scope = _app.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService(serviceType));
    }

    [Fact]
    public void Services_SeeTheCircuitsAccess_OnceTheLayoutStartsIt()
    {
        using var scope = _app.Services.CreateScope();

        Assert.IsType<CurrentAccess>(scope.ServiceProvider.GetRequiredService<ICurrentAccess>());
    }
}
