using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Hosting;
using SpeedtestWatcher.Core.SpeedTest;
using SpeedtestWatcher.Infrastructure.Network;
using SpeedtestWatcher.Infrastructure.SpeedTest;

namespace SpeedtestWatcher.Tests;

public sealed class ServerListProviderTests : IDisposable
{
    private const string DownloadedList = """[{"id":"2","name":"Leeds","sponsor":"Fresh Fibre","country":"United Kingdom","distance":120.5,"host":"leeds.example:8080"}]""";
    private const string CachedList = """[{"id":"1","name":"London","sponsor":"Old Fibre","country":"United Kingdom","distance":4.2,"host":"london.example:8080"}]""";

    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), "speedtest-watcher-tests", Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));
    private readonly RecordingHandler _handler = new() { ResponseBody = DownloadedList };
    private readonly ServerListProvider _provider;

    public ServerListProviderTests()
    {
        var options = Options.Create(new SpeedtestWatcherOptions { DataDirectory = _dataDirectory });
        _provider = new ServerListProvider(
            [new OoklaTool(), new LibreSpeedTool(), new CloudflareTool()],
            new StubHttpClientFactory(_handler),
            _time,
            options,
            NullLogger<ServerListProvider>.Instance);
    }

    private string CacheFile => Path.Combine(_dataDirectory, "servers", "ookla.json");

    [Fact]
    public async Task AFreshCache_IsUsedWithoutDownloading()
    {
        WriteCache(CachedList, age: TimeSpan.FromDays(6));

        var servers = await _provider.GetServersAsync(SpeedtestProvider.Ookla, TestContext.Current.CancellationToken);

        Assert.Equal("1", Assert.Single(servers!).Id);
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task ACacheOlderThanAWeek_IsDownloadedAgain()
    {
        WriteCache(CachedList, age: TimeSpan.FromDays(8));

        var servers = await _provider.GetServersAsync(SpeedtestProvider.Ookla, TestContext.Current.CancellationToken);

        Assert.Equal([new ServerInfo("2", "Leeds", "Fresh Fibre", "United Kingdom", 120.5, "leeds.example:8080")], servers);
        Assert.Contains("Leeds", await File.ReadAllTextAsync(CacheFile, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WhenTheDownloadFails_TheOldListIsStillUsed()
    {
        WriteCache(CachedList, age: TimeSpan.FromDays(30));
        _handler.Failure = new HttpRequestException("offline");

        var servers = await _provider.GetServersAsync(SpeedtestProvider.Ookla, TestContext.Current.CancellationToken);

        Assert.Equal("1", Assert.Single(servers!).Id);
    }

    [Fact]
    public async Task ACacheInTheFormatOfEarlierVersions_IsReplaced()
    {
        WriteCache("""{"1":{"name":"London","sponsor":"Old Fibre"}}""", age: TimeSpan.Zero);

        var servers = await _provider.GetServersAsync(SpeedtestProvider.Ookla, TestContext.Current.CancellationToken);

        Assert.Equal("2", Assert.Single(servers!).Id);
        Assert.Single(_handler.Requests);
    }

    [Fact]
    public async Task ProvidersWithoutServerChoice_HaveNoList()
    {
        Assert.Null(await _provider.GetServersAsync(SpeedtestProvider.Cloudflare, TestContext.Current.CancellationToken));
        Assert.Null(await _provider.GetServersAsync(SpeedtestProvider.None, TestContext.Current.CancellationToken));
    }

    private void WriteCache(string json, TimeSpan age)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CacheFile)!);
        File.WriteAllText(CacheFile, json);
        File.SetLastWriteTimeUtc(CacheFile, _time.GetUtcNow().UtcDateTime - age);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dataDirectory)) Directory.Delete(_dataDirectory, recursive: true);
    }
}
