using System.Net;
using SpeedtestWatcher.TestSupport;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models.Entities;
using SpeedtestWatcher.Application.Services;

namespace SpeedtestWatcher.UnitTests.Application.Services;

public class IntegrationSendTestTests
{
    private static readonly Speedtest _sample = new()
    {
        Ping = 12, Jitter = 0.4, Download = 941.25, Upload = 110.5, Status = TestStatus.Completed, Healthy = true,
        ServerName = "Acme Fibre", Created = new DateTime(2026, 9, 16, 8, 5, 0)
    };

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

    private static (RecordingHandler Handler, InMemoryIntegrations Repository, IntegrationDispatchService Dispatcher) Build()
    {
        var handler = new RecordingHandler();
        var repository = new InMemoryIntegrations([]);
        return (handler, repository, TestIntegrations.Dispatcher(repository, handler));
    }
}
