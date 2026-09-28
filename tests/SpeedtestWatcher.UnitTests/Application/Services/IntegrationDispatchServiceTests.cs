using System.Globalization;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.TestSupport;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Installers;
using SpeedtestWatcher.Application.Models.Entities;
using SpeedtestWatcher.Application.Models.Events;
using SpeedtestWatcher.Application.Repositories.Interfaces;
using SpeedtestWatcher.Application.Services;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.UnitTests.Application.Services;

public class IntegrationDispatchServiceTests
{
    private const string DiscordSettings = """{"url":"https://localhost/discord.com/api/webhooks/1/x"}""";

    [Fact]
    public async Task UnreadableSettingsAreNotSentAndTheFailureIsLogged()
    {
        var handler = new RecordingHandler();
        var repository = new InMemoryIntegrations([new IntegrationData { Id = "abc", Name = "discord", Data = "{\"url\": " }]);
        var logger = new RecordingLogger<IntegrationDispatchService>();
        var dispatcher = TestIntegrations.Dispatcher(repository, handler, logger);

        await dispatcher.PublishAsync(new TestFinished(new Speedtest { Download = 900 }), TestContext.Current.CancellationToken);

        Assert.Empty(handler.Requests);
        Assert.Equal([true], repository.ActivityErrors.ToArray());
        Assert.Contains(logger.Warnings, message => message.Contains("discord (abc)") && message.Contains("can't be read"));
    }

    [Fact]
    public async Task FailedDeliveriesAreLoggedWithTheReason()
    {
        var handler = new RecordingHandler { ResponseStatus = HttpStatusCode.InternalServerError, ResponseBody = "upstream exploded" };
        var repository = new InMemoryIntegrations([new IntegrationData { Id = "abc", Name = "discord", Data = DiscordSettings }]);
        var logger = new RecordingLogger<IntegrationDispatchService>();
        var dispatcher = TestIntegrations.Dispatcher(repository, handler, logger);

        await dispatcher.PublishAsync(new TestFinished(new Speedtest { Download = 900 }), TestContext.Current.CancellationToken);

        Assert.Equal([true], repository.ActivityErrors.ToArray());
        Assert.Contains(logger.Warnings, message => message.Contains("discord (abc)") && message.Contains("HTTP 500") && message.Contains("upstream exploded"));
    }

    [Fact]
    public async Task NumbersUseADotAsTheDecimalSeparatorWhateverTheServerLanguage()
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
    public async Task LastRunOnlyChangesWhenAnIntegrationSendsSomethingOrFails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler();
        var repository = new InMemoryIntegrations([new IntegrationData { Id = "abc", Name = "discord", Data = DiscordSettings }]);
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
    public async Task TheHeartbeatIntervalIsKeptEvenThoughEveryMinuteUsesANewScope(int intervalMinutes, int expectedPings)
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
            await scope.ServiceProvider.GetRequiredService<IIntegrationDispatchService>().PublishAsync(new Heartbeat(), cancellationToken);
        }

        Assert.Equal(expectedPings, handler.Requests.Count);
    }

    private static (RecordingHandler Handler, IntegrationDispatchService Dispatcher) Build(params (string Name, string Data)[] integrations)
    {
        var handler = new RecordingHandler();
        var repository = new InMemoryIntegrations(integrations.Select(i => new IntegrationData { Name = i.Name, Data = i.Data }).ToList());
        return (handler, TestIntegrations.Dispatcher(repository, handler));
    }
}
