using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SpeedtestWatcher.Tests;

public sealed class ConfigEndpointTests : IClassFixture<SignInOffApp>
{
    private readonly SignInOffApp _app;

    public ConfigEndpointTests(SignInOffApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task SavingSeveralSettings_ChangesAllOfThem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClientWithoutRedirects();

        using var response = await client.PatchAsJsonAsync("/api/config", new Dictionary<string, string> { ["dateFormat"] = "ymd", ["upload"] = "75" }, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var config = await ConfigAsync(client, cancellationToken);
        Assert.Equal(("ymd", "75"), (config.GetProperty("dateFormat").GetString(), config.GetProperty("upload").GetString()));
    }

    [Fact]
    public async Task OneInvalidValue_RejectsTheWholeBatch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClientWithoutRedirects();

        using var response = await client.PatchAsJsonAsync("/api/config", new Dictionary<string, string> { ["chartRange"] = "30d", ["cron"] = "whenever" }, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("You need to provide a valid cron expression", await MessageAsync(response, cancellationToken));
        Assert.Equal("7d", (await ConfigAsync(client, cancellationToken)).GetProperty("chartRange").GetString());
    }

    [Theory]
    [InlineData("""{"authEnabled":"true"}""", "Sign-in settings are changed on the Security tab")]
    [InlineData("""{"madeUpSetting":"1"}""", "There's no setting called madeUpSetting")]
    [InlineData("{}", "You need to provide at least one setting")]
    public async Task SignInSettings_UnknownKeys_AndEmptyBatches_AreRefused(string body, string expectedMessage)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClientWithoutRedirects();

        using var response = await client.PatchAsync("/api/config", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(expectedMessage, await MessageAsync(response, cancellationToken));
    }

    [Fact]
    public async Task TheSingleKeyEndpoint_IsGone()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClientWithoutRedirects();

        using var response = await client.PatchAsJsonAsync("/api/config/scheduleOffset", new { value = "false" }, cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("true", (await ConfigAsync(client, cancellationToken)).GetProperty("scheduleOffset").GetString());
    }

    private static async Task<JsonElement> ConfigAsync(HttpClient client, CancellationToken cancellationToken) =>
        await client.GetFromJsonAsync<JsonElement>("/api/config", cancellationToken);

    private static async Task<string?> MessageAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken)).GetProperty("message").GetString();
}
