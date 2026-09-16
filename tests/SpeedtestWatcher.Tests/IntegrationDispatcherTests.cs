using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Integrations;

namespace SpeedtestWatcher.Tests;

public class IntegrationDispatcherTests
{
    private static readonly Speedtest SkippedTest = new()
    {
        Status = "skipped",
        Error = "Public IP 203.0.113.9 is on the skip list"
    };

    private static readonly Dictionary<string, string> MinimalConfigs = new()
    {
        ["discord"] = """{"url":"https://localhost/discord.com/api/webhooks/1/x"}""",
        ["telegram"] = """{"token":"1:abc","chat_id":"42"}""",
        ["gotify"] = """{"url":"https://localhost/gotify","key":"AAAAAAAAAAAAAAA","priority":"5"}""",
        ["ntfy"] = """{"url":"https://localhost/ntfy","topic":"alerts"}""",
        ["pushover"] = """{"token":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","user_key":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"}""",
        ["webhook"] = """{"url":"https://localhost/hook"}"""
    };

    [Theory]
    [InlineData("discord")]
    [InlineData("telegram")]
    [InlineData("gotify")]
    [InlineData("ntfy")]
    [InlineData("pushover")]
    [InlineData("webhook")]
    public async Task SkippedTest_IsSentToEveryNotificationIntegration_ByDefault(string name)
    {
        var (handler, dispatcher) = Build((name, MinimalConfigs[name]));

        await dispatcher.TriggerEventAsync(IntegrationEvent.TestSkipped, SkippedTest, TestContext.Current.CancellationToken);

        var request = Assert.Single(handler.Requests);
        Assert.Contains(name == "webhook" ? "TEST_SKIPPED" : "skip list", request.Body);
    }

    [Fact]
    public async Task SkippedTest_IsNotSent_WhenTurnedOff()
    {
        var (handler, dispatcher) = Build(("discord", """{"url":"https://localhost/discord.com/api/webhooks/1/x","send_skipped":false}"""));

        await dispatcher.TriggerEventAsync(IntegrationEvent.TestSkipped, SkippedTest, TestContext.Current.CancellationToken);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SkippedTest_UsesTheCustomMessage()
    {
        var (handler, dispatcher) = Build(("ntfy", """{"url":"https://localhost/ntfy","topic":"alerts","skipped_message":"Sat out: %error%"}"""));

        await dispatcher.TriggerEventAsync(IntegrationEvent.TestSkipped, SkippedTest, TestContext.Current.CancellationToken);

        Assert.Contains("Sat out: Public IP 203.0.113.9 is on the skip list", Assert.Single(handler.Requests).Body);
    }

    [Fact]
    public async Task SkippedTest_IsLoggedInHealthchecks_WithoutSignallingSuccess()
    {
        var (handler, dispatcher) = Build(("healthChecks", """{"url":"https://localhost/hc/uuid"}"""));

        await dispatcher.TriggerEventAsync(IntegrationEvent.TestSkipped, SkippedTest, TestContext.Current.CancellationToken);

        Assert.Equal("https://localhost/hc/uuid/log", Assert.Single(handler.Requests).Uri);
    }

    [Theory]
    [InlineData(IntegrationEvent.TestFinished, "A speedtest is finished", 4572762)]
    [InlineData(IntegrationEvent.TestFailed, "A speedtest has failed", 12993861)]
    [InlineData(IntegrationEvent.TestUnhealthy, "A speedtest missed your targets", 16098851)]
    public async Task Discord_StillSendsEachExistingAlert(IntegrationEvent eventType, string heading, int color)
    {
        var (handler, dispatcher) = Build(("discord", MinimalConfigs["discord"]));

        await dispatcher.TriggerEventAsync(eventType, new Speedtest { Ping = 12, Download = 900, Upload = 100, Error = "Network unreachable" }, TestContext.Current.CancellationToken);

        var body = Assert.Single(handler.Requests).Body;
        Assert.Contains(heading, body);
        Assert.Contains($"\"color\":{color}", body);
    }

    [Fact]
    public async Task UnreadableSettings_AreNotSent_AndTheFailureIsLogged()
    {
        var handler = new RecordingHandler();
        var repository = new InMemoryIntegrations([new IntegrationData { Id = "abc", Name = "discord", Data = "{\"url\": " }]);
        var logger = new RecordingLogger<IntegrationDispatcher>();
        var dispatcher = new IntegrationDispatcher(repository, new StubHttpClientFactory(handler), logger);

        await dispatcher.TriggerEventAsync(IntegrationEvent.TestFinished, new Speedtest { Download = 900 }, TestContext.Current.CancellationToken);

        Assert.Empty(handler.Requests);
        Assert.Equal([true], repository.ActivityErrors.ToArray());
        Assert.Contains(logger.Warnings, message => message.Contains("discord (abc)") && message.Contains("can't be read"));
    }

    private static (RecordingHandler Handler, IntegrationDispatcher Dispatcher) Build(params (string Name, string Data)[] integrations)
    {
        var handler = new RecordingHandler();
        var repository = new InMemoryIntegrations(integrations.Select(i => new IntegrationData { Name = i.Name, Data = i.Data }).ToList());
        return (handler, new IntegrationDispatcher(repository, new StubHttpClientFactory(handler), NullLogger<IntegrationDispatcher>.Instance));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<(string Uri, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.RequestUri!.ToString(), body));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class InMemoryIntegrations(List<IntegrationData> items) : IIntegrationRepository
    {
        public List<bool> ActivityErrors { get; } = [];

        public Task<List<IntegrationData>> ListAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(items);

        public Task UpdateActivityAsync(string id, bool error, CancellationToken cancellationToken = default)
        {
            ActivityErrors.Add(error);
            return Task.CompletedTask;
        }

        public Task<List<IntegrationData>> GetByNameAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IntegrationData?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> CreateAsync(string name, string displayName, string dataJson, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> PatchAsync(string id, string? displayName, string dataJson, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpsertAsync(IntegrationData integration, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ClearAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
