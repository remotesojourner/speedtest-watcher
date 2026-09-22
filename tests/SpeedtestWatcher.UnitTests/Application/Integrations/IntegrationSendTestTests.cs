using System.Net;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.TestSupport;

namespace SpeedtestWatcher.UnitTests.Application.Integrations;

public class IntegrationSendTestTests
{
    private static readonly Speedtest _sample = new()
    {
        Ping = 12, Jitter = 0.4, Download = 941.25, Upload = 110.5, Status = TestStatus.Completed, Healthy = true,
        ServerName = "Acme Fibre", Created = new DateTime(2026, 9, 16, 8, 5, 0)
    };

    [Theory]
    [InlineData("discord", """{"url":"https://localhost/discord.com/api/webhooks/1/x","send_finished":false}""", "941.25 Mbps")]
    [InlineData("telegram", """{"token":"1:abc","chat_id":"42","send_finished":false}""", "941.25 Mbps")]
    [InlineData("gotify", """{"url":"https://localhost/gotify","key":"AAAAAAAAAAAAAAA","send_finished":false}""", "941.25 Mbps")]
    [InlineData("ntfy", """{"url":"https://localhost/ntfy","topic":"alerts","send_finished":false}""", "941.25 Mbps")]
    [InlineData("pushover", """{"token":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","user_key":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","send_finished":false}""", "941.25 Mbps")]
    [InlineData("apprise", """{"url":"https://localhost/apprise","urls":"json://localhost/hook","send_finished":false}""", "941.25 Mbps")]
    [InlineData("webhook", """{"url":"https://localhost/hook","send_finished":false}""", "\"event\":\"TEST\"")]
    [InlineData("healthChecks", """{"url":"https://localhost/hc/uuid"}""", "Acme Fibre")]
    public async Task SendTestSendsOneSampleEvenWhenThatMessageIsTurnedOff(string name, string settings, string expectedInBody)
    {
        var (handler, repository, dispatcher) = Build();

        var result = await dispatcher.TestAsync(name, "abc", settings, _sample, TestContext.Current.CancellationToken);

        Assert.Equal(IntegrationOutcome.Sent, result.Outcome);
        Assert.Contains(expectedInBody, Assert.Single(handler.Requests).Body);
        Assert.Empty(repository.ActivityErrors);
    }

    [Fact]
    public async Task HealthchecksSendTestOnlyLogsSoTheCheckKeepsItsState()
    {
        var (handler, _, dispatcher) = Build();

        await dispatcher.TestAsync("healthChecks", "abc", """{"url":"https://localhost/hc/uuid/"}""", _sample, TestContext.Current.CancellationToken);

        Assert.Equal("https://localhost/hc/uuid/log", Assert.Single(handler.Requests).Uri);
    }

    [Fact]
    public async Task InfluxDbSendTestChecksTheBucketWithoutWritingAPoint()
    {
        var (handler, _, dispatcher) = Build();
        handler.ResponseBody = """{"buckets":[{"id":"1","name":"speed"}]}""";

        var result = await dispatcher.TestAsync("influxdb", "abc", """{"url":"https://localhost/influx/","org":"home","bucket":"speed","token":"influx-token"}""", _sample, TestContext.Current.CancellationToken);

        Assert.Equal(IntegrationOutcome.Sent, result.Outcome);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(("GET", "https://localhost/influx/api/v2/buckets?org=home&name=speed"), (request.Method, request.Uri));
        Assert.Contains("Authorization: Token influx-token", request.Headers);
    }

    [Fact]
    public async Task InfluxDbSendTestFailsWhenTheBucketDoesNotExist()
    {
        var (handler, _, dispatcher) = Build();
        handler.ResponseBody = """{"buckets":[]}""";

        var result = await dispatcher.TestAsync("influxdb", "abc", """{"url":"https://localhost/influx","org":"home","bucket":"speed","token":"t"}""", _sample, TestContext.Current.CancellationToken);

        Assert.Equal(IntegrationOutcome.Failed, result.Outcome);
        Assert.Contains("speed", result.Error);
    }

    [Fact]
    public async Task SendTestReportsTheStatusAndWhatTheServiceSaid()
    {
        var (handler, _, dispatcher) = Build();
        handler.ResponseStatus = HttpStatusCode.Unauthorized;
        handler.ResponseBody = """{"error":"unauthorized","errorDescription":"you need to provide a valid access token"}""";

        var result = await dispatcher.TestAsync("gotify", "abc", """{"url":"https://localhost/gotify","key":"AAAAAAAAAAAAAAA"}""", _sample, TestContext.Current.CancellationToken);

        Assert.Equal(IntegrationOutcome.Failed, result.Outcome);
        Assert.Contains("HTTP 401", result.Error);
        Assert.Contains("valid access token", result.Error);
    }

    [Fact]
    public async Task SendTestReportsWhenTheServiceCannotBeReached()
    {
        var (handler, _, dispatcher) = Build();
        handler.Failure = new HttpRequestException("No connection could be made because the target machine actively refused it.");

        var result = await dispatcher.TestAsync("ntfy", "abc", """{"url":"https://localhost/ntfy","topic":"alerts"}""", _sample, TestContext.Current.CancellationToken);

        Assert.Equal(IntegrationOutcome.Failed, result.Outcome);
        Assert.Contains("actively refused", result.Error);
    }

    [Theory]
    [InlineData("carrierPigeon", """{"url":"https://localhost/coop"}""", "isn't a known integration type")]
    [InlineData("discord", """{"display_name":"Watcher"}""", "webhook URL is missing")]
    [InlineData("discord", """not json""", "can't be read")]
    public async Task SendTestExplainsSettingsItCannotUse(string name, string settings, string expectedError)
    {
        var (handler, _, dispatcher) = Build();

        var result = await dispatcher.TestAsync(name, "abc", settings, _sample, TestContext.Current.CancellationToken);

        Assert.Equal(IntegrationOutcome.Failed, result.Outcome);
        Assert.Contains(expectedError, result.Error);
        Assert.Empty(handler.Requests);
    }

    private static (RecordingHandler Handler, InMemoryIntegrations Repository, IntegrationDispatcher Dispatcher) Build()
    {
        var handler = new RecordingHandler();
        var repository = new InMemoryIntegrations([]);
        return (handler, repository, TestIntegrations.Dispatcher(repository, handler));
    }
}
