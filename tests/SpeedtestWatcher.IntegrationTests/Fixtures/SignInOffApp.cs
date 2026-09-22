namespace SpeedtestWatcher.IntegrationTests.Fixtures;

public sealed class SignInOffApp : TestApp
{
    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await SampleData.ChooseOoklaAsync(services, cancellationToken);
        await SampleData.SeedResultsAsync(services, cancellationToken);
    }
}
