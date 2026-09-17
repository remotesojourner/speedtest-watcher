using System.Net;
using System.Text.Json;
using SpeedtestWatcher.Core.Events;
using SpeedtestWatcher.Core.Integrations;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Integrations;

namespace SpeedtestWatcher.Tests;

public class AppriseIntegrationTests
{
    private const string WithUrls = """{"url":"http://apprise:8000/","urls":"json://listener/hook"}""";
    private const string WithKey = """{"url":"http://apprise:8000","key":"home"}""";

    private static readonly Speedtest Result = new()
    {
        Ping = 12, Jitter = 0.4, Download = 941.25, Upload = 110.5, Status = "completed", Healthy = true, Error = "Network unreachable"
    };

    private readonly RecordingHandler _handler = new();

    public static TheoryData<string, IntegrationEvent> MessageTypes => new()
    {
        { "success", new TestFinished(Result) },
        { "failure", new TestFailed(Result) },
        { "warning", new TestUnhealthy(Result) },
        { "info", new TestSkipped(Result) }
    };

    [Theory]
    [MemberData(nameof(MessageTypes))]
    public async Task EachMessage_IsSentWithTheMatchingAppriseType(string expectedType, IntegrationEvent integrationEvent)
    {
        var repository = new InMemoryIntegrations([new IntegrationData { Id = "abc", Name = "apprise", Data = WithUrls }]);

        await TestIntegrations.Dispatcher(repository, _handler).PublishAsync(integrationEvent, TestContext.Current.CancellationToken);

        using var body = JsonDocument.Parse(Assert.Single(_handler.Requests).Body);
        Assert.Equal(expectedType, body.RootElement.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(WithUrls, "Apprise found no valid URLs to send to")]
    [InlineData(WithKey, "Apprise has no configuration for the key home")]
    public async Task NothingToSendTo_IsAFailure_ThoughAppriseAnswers204(string settings, string expectedError)
    {
        _handler.ResponseStatus = HttpStatusCode.NoContent;

        var result = await SendTestAsync(settings);

        Assert.Equal(IntegrationResult.Failed(expectedError), result);
    }

    [Fact]
    public async Task AFailedDelivery_ShowsWhatAppriseLogged()
    {
        _handler.ResponseStatus = HttpStatusCode.FailedDependency;
        _handler.ResponseBody = """{"error": "One or more notifications could not be sent", "details": [["INFO", "2026-09-17 03:28:04,050", "Notifying 2 service(s) with threads."], ["WARNING", "2026-09-17 03:28:04,135", "Failed to send JSON POST notification: Verification Failed., error=401."]]}""";

        var result = await SendTestAsync(WithUrls);

        Assert.Equal(IntegrationResult.Failed("Apprise answered HTTP 424: One or more notifications could not be sent: Failed to send JSON POST notification: Verification Failed., error=401."), result);
    }

    [Theory]
    [InlineData("""{"url":"http://apprise:8000","key":"home","tags":"admin devops"}""", "Apprise has nothing tagged admin devops to notify")]
    [InlineData(WithKey, "Apprise has nothing untagged to notify. Add tags, or all, to choose what to notify")]
    public async Task TagsThatMatchNothing_AreExplained(string settings, string expectedError)
    {
        _handler.ResponseStatus = HttpStatusCode.FailedDependency;
        _handler.ResponseBody = """{"error": "One or more notification could not be sent", "details": []}""";

        var result = await SendTestAsync(settings);

        Assert.Equal(IntegrationResult.Failed(expectedError), result);
    }

    [Fact]
    public async Task ARejectedRequest_ShowsApprisesError()
    {
        _handler.ResponseStatus = HttpStatusCode.BadRequest;
        _handler.ResponseBody = """{"error": "Payload lacks minimum requirements"}""";

        var result = await SendTestAsync(WithKey);

        Assert.Equal(IntegrationResult.Failed("Apprise answered HTTP 400: Payload lacks minimum requirements"), result);
    }

    [Fact]
    public async Task UrlsAndAConfigKeyTogether_AreRefused_WithoutSending()
    {
        var result = await SendTestAsync("""{"url":"http://apprise:8000","urls":"json://listener/hook","key":"home"}""");

        Assert.Equal(IntegrationResult.Failed("Use either Apprise URLs or a config key, not both"), result);
        Assert.Empty(_handler.Requests);
    }

    private Task<IntegrationResult> SendTestAsync(string settings) =>
        TestIntegrations.Dispatcher(new InMemoryIntegrations([]), _handler)
            .TestAsync("apprise", "abc", settings, Result, TestContext.Current.CancellationToken);
}
