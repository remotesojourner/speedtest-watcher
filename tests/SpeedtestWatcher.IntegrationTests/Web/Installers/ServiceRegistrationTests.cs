using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.Application.Services;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Web.Services;

namespace SpeedtestWatcher.IntegrationTests.Web.Installers;

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
        typeof(ICurrentAccessService),
        typeof(CircuitAccessService),
        typeof(HttpCurrentAccessService),
        typeof(PreferencesService),
        typeof(StatusStateService),
        typeof(SettingsStateService),
        typeof(RecentResultsService),
        typeof(LiveUpdatesService),
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
    public void ServicesSeeTheCircuitsAccessOnceTheLayoutStartsIt()
    {
        using var scope = _app.Services.CreateScope();

        Assert.IsType<CurrentAccessService>(scope.ServiceProvider.GetRequiredService<ICurrentAccessService>());
    }
}
