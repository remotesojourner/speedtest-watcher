using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace SpeedtestWatcher.IntegrationTests.Fixtures;

public sealed class SignInOffApp : TestApp
{
    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await SampleData.ChooseOoklaAsync(services, cancellationToken);
        await SampleData.SeedResultsAsync(services, cancellationToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(new FakeTimeProvider(SampleData.ResultsViewedAt)));
    }
}
