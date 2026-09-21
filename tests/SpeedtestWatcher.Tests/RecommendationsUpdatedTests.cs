using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Events;
using SpeedtestWatcher.Core.Integrations;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Data;
using SpeedtestWatcher.Infrastructure.Network;
using SpeedtestWatcher.Infrastructure.Repositories;
using SpeedtestWatcher.Infrastructure.SpeedTest;
using SpeedtestWatcher.Web.Background;

namespace SpeedtestWatcher.Tests;

public class RecommendationsUpdatedTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private ServiceProvider? _services;

    [Fact(Timeout = 15000)]
    public async Task CompletedTestsBeforeTheTenth_StoreNoRecommendation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scheduler = await BuildSchedulerAsync(new RecordingDispatcher(), cancellationToken);

        for (var run = 1; run < RecommendationRepository.CompletedTestsNeeded; run++)
        {
            var result = await scheduler.ExecuteSpeedtestAsync(TestType.Custom, cancellationToken: cancellationToken);
            Assert.True(result.Success, result.Error);
        }

        using var scope = _services!.CreateScope();
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IRecommendationRepository>().GetAsync(cancellationToken));
    }

    [Fact(Timeout = 15000)]
    public async Task TheTenthCompletedTest_PublishesTheNewRecommendations_AndUnchangedValuesPublishNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var published = new RecordingDispatcher();
        var scheduler = await BuildSchedulerAsync(published, cancellationToken);

        for (var run = 1; run <= 11; run++)
        {
            var result = await scheduler.ExecuteSpeedtestAsync(TestType.Custom, cancellationToken: cancellationToken);
            Assert.True(result.Success, result.Error);
        }

        var update = Assert.Single(published.Events.OfType<RecommendationsUpdated>());
        Assert.Equal((10, 950.5, 115.25), (update.Recommendation.Ping, update.Recommendation.Download, update.Recommendation.Upload));

        var tenthFinished = published.Events.Select((integrationEvent, index) => (integrationEvent, index)).Where(e => e.integrationEvent is TestFinished).ElementAt(9).index;
        Assert.Same(update, published.Events[tenthFinished + 1]);
    }

    private async Task<SpeedtestSchedulerService> BuildSchedulerAsync(IIntegrationDispatcher dispatcher, CancellationToken cancellationToken)
    {
        _connection.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSignalR();
        services.AddHttpClient();
        services.AddDbContext<SpeedtestWatcherDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<ISettingsStore, SettingsStore>();
        services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddSingleton(dispatcher);
        services.AddSingleton<ServerListProvider>();
        services.AddScoped<ServerSelector>();
        services.AddScoped<ConnectivityChecker>();
        services.AddSingleton<ISpeedtestRunner, SteadyRunner>();
        services.AddSingleton<IPauseStateService, PauseStateService>();
        services.AddSingleton<SpeedtestSchedulerService>();
        var provider = services.BuildServiceProvider();
        _services = provider;

        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<SpeedtestWatcherDbContext>().Database.EnsureCreatedAsync(cancellationToken);
            var settings = scope.ServiceProvider.GetRequiredService<ISettingsStore>();
            await settings.InsertDefaultsAsync(cancellationToken);
            await settings.SaveAsync(new Dictionary<string, string> { ["provider"] = "ookla", ["internetCheckEnabled"] = "false" }, cancellationToken);
        }

        return provider.GetRequiredService<SpeedtestSchedulerService>();
    }

    public void Dispose()
    {
        _services?.Dispose();
        _connection.Dispose();
    }

    private sealed class SteadyRunner : ISpeedtestRunner
    {
        public Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SpeedtestExecutionResult { Success = true, Ping = 10, Download = 950.5, Upload = 115.25 });
    }

    private sealed class RecordingDispatcher : IIntegrationDispatcher
    {
        private readonly object _gate = new();
        private readonly List<IntegrationEvent> _events = [];

        public IReadOnlyList<IntegrationEvent> Events
        {
            get
            {
                lock (_gate) return _events.ToList();
            }
        }

        public IReadOnlyDictionary<string, IntegrationTypeSchemaDto> Schemas { get; } = new Dictionary<string, IntegrationTypeSchemaDto>();

        public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            lock (_gate) _events.Add(integrationEvent);
            return Task.CompletedTask;
        }

        public Task<IntegrationResult> TestAsync(string name, string id, string settingsJson, Speedtest sample, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
