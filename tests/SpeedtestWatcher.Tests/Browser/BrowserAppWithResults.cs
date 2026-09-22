namespace SpeedtestWatcher.Tests.Browser;

public sealed class BrowserAppWithResults : BrowserApp
{
    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await SampleData.ChooseOoklaAsync(services, cancellationToken);
        await SampleData.SeedRecentResultsAsync(services, cancellationToken);
    }
}
