using System.Net;
using System.Runtime.CompilerServices;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.Web.Startup;

namespace SpeedtestWatcher.IntegrationTests.Web.Api;

public sealed class HealthEndpointTests : IClassFixture<NoVisitorsApp>, IClassFixture<SignInOffApp>
{
    private readonly NoVisitorsApp _noVisitors;
    private readonly SignInOffApp _signInOff;

    public HealthEndpointTests(NoVisitorsApp noVisitors, SignInOffApp signInOff)
    {
        _noVisitors = noVisitors;
        _signInOff = signInOff;
    }

    [Fact]
    public async Task AWorkingAppAnswersTheHealthCheck()
    {
        using var client = _signInOff.CreateClientWithoutRedirects();

        using var response = await client.GetAsync(HealthEndpoint.Path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void TheDockerImageAsksTheSameEndpoint()
    {
        var dockerfile = File.ReadAllText(Path.Combine(Path.GetDirectoryName(ThisFile())!, "../../../../Dockerfile"));

        Assert.Contains("HEALTHCHECK", dockerfile, StringComparison.Ordinal);
        Assert.Contains(HealthEndpoint.Path, dockerfile, StringComparison.Ordinal);
    }

    private static string ThisFile([CallerFilePath] string path = "") => path;

    [Fact]
    public async Task TheHealthCheckAnswersEvenWhereVisitorsAreRefusedEverythingElse()
    {
        using var client = _noVisitors.CreateClientWithoutRedirects();

        using var health = await client.GetAsync(HealthEndpoint.Path, TestContext.Current.CancellationToken);
        using var page = await client.GetAsync("/history", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
    }
}
