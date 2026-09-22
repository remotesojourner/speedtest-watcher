using System.Net;
using System.Net.Http.Json;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Web.Api;

public sealed class PauseTests : IClassFixture<SignInOffApp>
{
    private readonly SignInOffApp _app;

    public PauseTests(SignInOffApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task APauseLongerThan30Days_IsRefused_AndTestsKeepRunning()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClientWithoutRedirects();

        using var response = await client.PostAsJsonAsync("/api/speedtests/pause", new { resumeIn = 721 }, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("at most 720 hours", await response.Content.ReadAsStringAsync(cancellationToken), StringComparison.Ordinal);
        Assert.False((await StatusAsync(client, cancellationToken)).Paused);
    }

    [Fact]
    public async Task APauseOf30Days_IsAccepted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClientWithoutRedirects();

        using var paused = await client.PostAsJsonAsync("/api/speedtests/pause", new { resumeIn = 720 }, cancellationToken);
        var statusWhilePaused = await StatusAsync(client, cancellationToken);
        using var resumed = await client.PostAsync("/api/speedtests/continue", null, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, paused.StatusCode);
        Assert.True(statusWhilePaused.Paused);
        Assert.False((await StatusAsync(client, cancellationToken)).Paused);
    }

    private static async Task<StatusDto> StatusAsync(HttpClient client, CancellationToken cancellationToken) =>
        (await client.GetFromJsonAsync<StatusDto>("/api/speedtests/status", cancellationToken))!;
}
