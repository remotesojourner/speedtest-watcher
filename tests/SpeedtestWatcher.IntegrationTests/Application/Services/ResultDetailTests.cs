using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.TestSupport;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Repositories;
using SpeedtestWatcher.Application.Services;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.IntegrationTests.Application.Services;

public sealed class ResultDetailTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private ServiceProvider? _services;

    [Fact(Timeout = 15000)]
    public async Task AResultKeepsWhatTheToolAndTheCheckReported()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var run = await RunOnceAsync(cancellationToken);

        Assert.True(run.Success);
        await using var db = _database.NewContext();
        var stored = Assert.Single(await new SpeedtestRepository(db).ListAllAsync(cancellationToken));
        Assert.Equal((0.5, 900_000_000L, 90_000_000L, "203.0.113.9"), (stored.PacketLoss, stored.DownloadBytes, stored.UploadBytes, stored.PublicIp));
    }

    [Fact(Timeout = 15000)]
    public async Task OnlyFullAccessSeesThePublicIp()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await RunOnceAsync(cancellationToken);
        await using var db = _database.NewContext();
        var repository = new SpeedtestRepository(db);

        var forOwner = await new ResultsService(repository, FixedAccess.Full).ListAsync(null, 10, null, null, null, cancellationToken);
        var forVisitor = await new ResultsService(repository, FixedAccess.ReadOnly).ListAsync(null, 10, null, null, null, cancellationToken);

        Assert.Equal("203.0.113.9", forOwner[0].PublicIp);
        Assert.Null(forVisitor[0].PublicIp);
        Assert.Equal(forOwner[0].DownloadBytes, forVisitor[0].DownloadBytes);
    }

    private async Task<SpeedtestExecutionResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        _services = await RunTestServices.BuildAsync(_database, new DetailedRunner(), cancellationToken,
            configure: services => services.AddSingleton<IConnectivityCheckService>(new KnownIpChecker()));
        var runService = _services.CreateScope().ServiceProvider.GetRequiredService<SpeedtestRunService>();
        return await runService.RunAsync(TestType.Custom, cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        _services?.Dispose();
        _database.Dispose();
    }

    private sealed class DetailedRunner : IToolRunnerService
    {
        public Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SpeedtestExecutionResult
            {
                Success = true,
                Ping = 10,
                Download = 900,
                Upload = 100,
                PacketLoss = 0.5,
                DownloadBytes = 900_000_000,
                UploadBytes = 90_000_000
            });
    }

    private sealed class KnownIpChecker : IConnectivityCheckService
    {
        public Task<PreTestCheck> CheckAsync(PreTestCheckSettings settings, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PreTestCheck(true, null, "203.0.113.9"));
    }
}
