using System.Net;
using System.Text.Json;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.TestSupport;

namespace SpeedtestWatcher.IntegrationTests.Application.Integrations;

public sealed class IntegrationServiceTests : IDisposable
{
    private const string WebhookSettings = """{"url":"https://localhost/hook","send_finished":false}""";

    private readonly TestDatabase _database = new();
    private readonly SpeedtestWatcherDbContext _db;
    private readonly RecordingHandler _handler = new();
    private readonly ISpeedtestRepository _speedtests = A.Fake<ISpeedtestRepository>();

    public IntegrationServiceTests()
    {
        _db = _database.NewContext();
    }

    [Fact]
    public async Task SendTest_UsesTheLatestCompletedResult_WithoutSavingAnything()
    {
        A.CallTo(() => _speedtests.GetLatestCompletedAsync(A<CancellationToken>._))
            .Returns(new Speedtest { ServerName = "Acme Fibre", Ping = 12, Download = 941.25, Upload = 110.5 });

        var result = await Service().SendTestAsync("webhook", Settings(WebhookSettings), "abc123", TestContext.Current.CancellationToken);

        Assert.Equal((true, null), (result.Value!.Success, result.Value.Message));
        Assert.Contains("Acme Fibre", Assert.Single(_handler.Requests).Body);
        Assert.Empty(await _db.Integrations.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendTest_UsesSampleValues_BeforeAnyTestHasCompleted()
    {
        A.CallTo(() => _speedtests.GetLatestCompletedAsync(A<CancellationToken>._)).Returns((Speedtest?)null);

        var result = await Service().SendTestAsync("webhook", Settings(WebhookSettings), null, TestContext.Current.CancellationToken);

        Assert.True(result.Value!.Success);
        Assert.Contains("Sample server", Assert.Single(_handler.Requests).Body);
    }

    [Fact]
    public async Task SendTest_AnswersWithTheReason_WhenTheServiceRefusesIt()
    {
        _handler.ResponseStatus = HttpStatusCode.NotFound;
        _handler.ResponseBody = "Unknown Webhook";

        var result = await Service().SendTestAsync(
            "discord", Settings("""{"url":"https://localhost/discord.com/api/webhooks/1/x"}"""), null, TestContext.Current.CancellationToken);

        Assert.Equal((false, "Discord answered HTTP 404: Unknown Webhook"), (result.Value!.Success, result.Value.Message));
    }

    [Fact]
    public async Task SendTest_IsRefused_WithoutFullAccess()
    {
        var result = await Service(FixedAccess.ReadOnly).SendTestAsync("webhook", Settings(WebhookSettings), null, TestContext.Current.CancellationToken);

        Assert.Equal(OperationOutcome.Denied, result.Outcome);
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task SendTest_ForAnUnknownType_IsNotFound()
    {
        var result = await Service().SendTestAsync("carrierPigeon", Settings(WebhookSettings), null, TestContext.Current.CancellationToken);

        Assert.Equal(OperationOutcome.NotFound, result.Outcome);
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task Create_StoresTheDisplayNameApartFromTheSettings()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var created = await Service().CreateAsync("webhook", "Home Assistant", Settings(WebhookSettings), cancellationToken);

        var stored = Assert.Single(await _db.Integrations.ToListAsync(cancellationToken));
        Assert.Equal((created.Value, "Home Assistant"), (stored.Id, stored.DisplayName));
        Assert.Equal(Settings(WebhookSettings).Keys.Order(), Settings(stored.Data).Keys.Order());
    }

    [Theory]
    [InlineData("""{"send_finished":true}""", "url is required")]
    [InlineData("""{"url":"not a web address"}""", "url doesn't have the expected format")]
    [InlineData("""{"url":"https://localhost/hook","interval":"soon"}""", "interval needs to be a whole number")]
    [InlineData("""{"url":"https://localhost/hook","send_finished":"yes"}""", "send_finished needs to be true or false")]
    [InlineData("""{"url":"https://localhost/hook","carrier":"pigeon"}""", "carrier isn't a setting of the Webhook integration")]
    public async Task Create_RefusesSettingsTheTypeCannotUse(string settings, string expectedProblem)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var result = await Service().CreateAsync("webhook", null, Settings(settings), cancellationToken);

        Assert.Equal((OperationOutcome.Invalid, expectedProblem), (result.Outcome, result.Message));
        Assert.Empty(await _db.Integrations.ToListAsync(cancellationToken));
    }

    [Fact]
    public async Task Update_ChecksTheSettingsItWouldSave()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = Service();
        var id = (await service.CreateAsync("webhook", "Home", Settings(WebhookSettings), cancellationToken)).Value!;

        var cleared = await service.UpdateAsync(id, null, Settings("""{"url":""}"""), cancellationToken);
        var toggled = await service.UpdateAsync(id, null, Settings("""{"send_finished":true}"""), cancellationToken);

        Assert.Equal((OperationOutcome.Invalid, "url is required"), (cleared.Outcome, cleared.Message));
        Assert.True(toggled.Succeeded);
        var stored = Settings(Assert.Single(await _db.Integrations.AsNoTracking().ToListAsync(cancellationToken)).Data);
        Assert.Equal(("https://localhost/hook", true), (stored["url"].GetString(), stored["send_finished"].GetBoolean()));
    }

    private IntegrationService Service(ICurrentAccess? access = null)
    {
        var repository = new IntegrationRepository(_db);
        return new IntegrationService(repository, TestIntegrations.Dispatcher(repository, _handler), _speedtests, access ?? FixedAccess.Full);
    }

    private static Dictionary<string, JsonElement> Settings(string json) => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }
}
