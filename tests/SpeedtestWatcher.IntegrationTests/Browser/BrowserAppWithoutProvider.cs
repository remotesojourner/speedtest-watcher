namespace SpeedtestWatcher.IntegrationTests.Browser;

public sealed class BrowserAppWithoutProvider : BrowserApp
{
    protected override Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;
}
