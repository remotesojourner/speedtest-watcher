using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Browser;

public sealed class BrowserAppForReadOnlyVisitors : BrowserApp
{
    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await SampleData.ChooseOoklaAsync(services, cancellationToken);
        await SampleData.SeedRecentResultsAsync(services, cancellationToken);
        await SampleData.TurnSignInOnAsync(services, VisitorAccess.Read, ApiToken.Generate(), cancellationToken);
    }
}
