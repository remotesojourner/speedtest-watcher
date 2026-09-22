using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Tests;

public class IntegrationDispatcherTests
{
    private static readonly Speedtest SkippedTest = new()
    {
        Status = TestStatus.Skipped,
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

        await dispatcher.PublishAsync(new TestSkipped(SkippedTest), TestContext.Current.CancellationToken);

        var request = Assert.Single(handler.Requests);
        Assert.Contains(name == "webhook" ? "TEST_SKIPPED" : "skip list", request.Body);
    }

    [Fact]
    public async Task SkippedTest_IsNotSent_WhenTurnedOff()
    {
        var (handler, dispatcher) = Build(("discord", """{"url":"https://localhost/discord.com/api/webhooks/1/x","send_skipped":false}"""));

        await dispatcher.PublishAsync(new TestSkipped(SkippedTest), TestContext.Current.CancellationToken);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SkippedTest_UsesTheCustomMessage()
    {
        var (handler, dispatcher) = Build(("ntfy", """{"url":"https://localhost/ntfy","topic":"alerts","skipped_message":"Sat out: %error%"}"""));

        await dispatcher.PublishAsync(new TestSkipped(SkippedTest), TestContext.Current.CancellationToken);

        Assert.Contains("Sat out: Public IP 203.0.113.9 is on the skip list", Assert.Single(handler.Requests).Body);
    }

    [Fact]
    public async Task SkippedTest_IsLoggedInHealthchecks_WithoutSignallingSuccess()
    {
        var (handler, dispatcher) = Build(("healthChecks", """{"url":"https://localhost/hc/uuid"}"""));

        await dispatcher.PublishAsync(new TestSkipped(SkippedTest), TestContext.Current.CancellationToken);

        Assert.Equal("https://localhost/hc/uuid/log", Assert.Single(handler.Requests).Uri);
    }

    [Theory]
    [InlineData("finished", "A speedtest is finished", 4572762)]
    [InlineData("failed", "A speedtest has failed", 12993861)]
    [InlineData("missed targets", "A speedtest missed your targets", 16098851)]
    public async Task Discord_StillSendsEachExistingAlert(string outcome, string heading, int color)
    {
        var (handler, dispatcher) = Build(("discord", MinimalConfigs["discord"]));
        var result = new Speedtest { Ping = 12, Download = 900, Upload = 100, Error = "Network unreachable" };
        IntegrationEvent integrationEvent = outcome switch
        {
            "finished" => new TestFinished(result),
            "failed" => new TestFailed(result),
            _ => new TestUnhealthy(result)
        };

        await dispatcher.PublishAsync(integrationEvent, TestContext.Current.CancellationToken);

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
        var dispatcher = TestIntegrations.Dispatcher(repository, handler, logger);

        await dispatcher.PublishAsync(new TestFinished(new Speedtest { Download = 900 }), TestContext.Current.CancellationToken);

        Assert.Empty(handler.Requests);
        Assert.Equal([true], repository.ActivityErrors.ToArray());
        Assert.Contains(logger.Warnings, message => message.Contains("discord (abc)") && message.Contains("can't be read"));
    }

    [Fact]
    public async Task FailedDeliveries_AreLogged_WithTheReason()
    {
        var handler = new RecordingHandler { ResponseStatus = HttpStatusCode.InternalServerError, ResponseBody = "upstream exploded" };
        var repository = new InMemoryIntegrations([new IntegrationData { Id = "abc", Name = "discord", Data = MinimalConfigs["discord"] }]);
        var logger = new RecordingLogger<IntegrationDispatcher>();
        var dispatcher = TestIntegrations.Dispatcher(repository, handler, logger);

        await dispatcher.PublishAsync(new TestFinished(new Speedtest { Download = 900 }), TestContext.Current.CancellationToken);

        Assert.Equal([true], repository.ActivityErrors.ToArray());
        Assert.Contains(logger.Warnings, message => message.Contains("discord (abc)") && message.Contains("HTTP 500") && message.Contains("upstream exploded"));
    }

    [Fact]
    public async Task AnIntegrationSavedFromTheSettingsForm_SendsTheDefaultMessage_NotABlankOne()
    {
        var (handler, dispatcher) = Build(("discord", """{"url":"https://localhost/discord.com/api/webhooks/1/x","display_name":"","send_finished":true,"finished_message":""}"""));

        await dispatcher.PublishAsync(new TestFinished(new Speedtest { Ping = 12, Download = 941.25, Upload = 110.5 }), TestContext.Current.CancellationToken);

        var body = Assert.Single(handler.Requests).Body;
        Assert.Contains("\"username\":\"Speedtest Watcher\"", body);
        Assert.Contains("A speedtest is finished", body);
    }

    [Fact]
    public async Task Numbers_UseADotAsTheDecimalSeparator_WhateverTheServerLanguage()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            var (handler, dispatcher) = Build(
                ("ntfy", """{"url":"https://localhost/ntfy","topic":"alerts"}"""),
                ("influxdb", """{"url":"https://localhost/influx","org":"home","bucket":"speed","token":"t","host":"watcher"}"""));

            await dispatcher.PublishAsync(new TestFinished(new Speedtest { Ping = 12, Jitter = 0.4, Download = 941.25, Upload = 110.5 }), TestContext.Current.CancellationToken);

            Assert.Equal(2, handler.Requests.Count);
            Assert.All(handler.Requests, request =>
            {
                Assert.Contains("941.25", request.Body);
                Assert.DoesNotContain("941,25", request.Body);
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public async Task FailureMessagesAndWebhooks_IncludeTheFailedResult()
    {
        var (handler, dispatcher) = Build(
            ("ntfy", """{"url":"https://localhost/ntfy","topic":"alerts","error_message":"%server%: %error%"}"""),
            ("webhook", """{"url":"https://localhost/hook"}"""));

        await dispatcher.PublishAsync(new TestFailed(new Speedtest { ServerName = "Acme Fibre", Status = TestStatus.Failed, Error = "Network unreachable" }), TestContext.Current.CancellationToken);

        Assert.Equal("Acme Fibre: Network unreachable", handler.Requests[0].Body);
        using var webhook = JsonDocument.Parse(handler.Requests[1].Body);
        Assert.Equal("Acme Fibre", webhook.RootElement.GetProperty("data").GetProperty("ServerName").GetString());
    }

    [Fact]
    public async Task LastRun_OnlyChanges_WhenAnIntegrationSendsSomethingOrFails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler();
        var repository = new InMemoryIntegrations([new IntegrationData { Id = "abc", Name = "discord", Data = MinimalConfigs["discord"] }]);
        var dispatcher = TestIntegrations.Dispatcher(repository, handler);

        await dispatcher.PublishAsync(new TestStarted(SpeedtestProvider.Ookla, TestType.Auto), cancellationToken);
        await dispatcher.PublishAsync(new Heartbeat(), cancellationToken);
        Assert.Empty(repository.ActivityErrors);

        await dispatcher.PublishAsync(new TestFinished(new Speedtest { Download = 900 }), cancellationToken);
        handler.ResponseStatus = HttpStatusCode.BadRequest;
        await dispatcher.PublishAsync(new TestFinished(new Speedtest { Download = 900 }), cancellationToken);

        Assert.Equal([false, true], repository.ActivityErrors.ToArray());
    }

    [Theory]
    [InlineData(5, 1)]
    [InlineData(1, 2)]
    public async Task TheHeartbeatInterval_IsKept_EvenThoughEveryMinuteUsesANewScope(int intervalMinutes, int expectedPings)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler();
        var healthchecks = new IntegrationData { Id = "hc", Name = "healthChecks", Data = $$"""{"url":"https://localhost/hc/uuid","interval":{{intervalMinutes}}}""" };
        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(handler))
            .AddSingleton<IIntegrationRepository>(new InMemoryIntegrations([healthchecks]))
            .AddIntegrations()
            .BuildServiceProvider();

        foreach (var _ in Enumerable.Range(0, 2))
        {
            await using var scope = provider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IIntegrationDispatcher>().PublishAsync(new Heartbeat(), cancellationToken);
        }

        Assert.Equal(expectedPings, handler.Requests.Count);
    }

    [Fact]
    public async Task InfluxDb_StampsThePointWithTheTestTime_AndAddsTheConfiguredTags()
    {
        var tested = new DateTime(2026, 9, 16, 8, 5, 0);
        var (handler, dispatcher) = Build(("influxdb", """{"url":"https://localhost/influx","org":"home","bucket":"speed","token":"t","host":"living room","tags":"env=prod, site=home office"}"""));

        await dispatcher.PublishAsync(new TestFinished(new Speedtest { Ping = 12, Jitter = 0.4, Download = 941.25, Upload = 110.5, Created = tested }), TestContext.Current.CancellationToken);

        var testedSeconds = new DateTimeOffset(tested, TimeSpan.Zero).ToUnixTimeSeconds();
        Assert.Equal(
            $@"speedtests,host=living\ room,env=prod,site=home\ office download=941.25,upload=110.50,ping=12,jitter=0.40 {testedSeconds}",
            Assert.Single(handler.Requests).Body);
    }

    private static (RecordingHandler Handler, IntegrationDispatcher Dispatcher) Build(params (string Name, string Data)[] integrations)
    {
        var handler = new RecordingHandler();
        var repository = new InMemoryIntegrations(integrations.Select(i => new IntegrationData { Name = i.Name, Data = i.Data }).ToList());
        return (handler, TestIntegrations.Dispatcher(repository, handler));
    }
}
