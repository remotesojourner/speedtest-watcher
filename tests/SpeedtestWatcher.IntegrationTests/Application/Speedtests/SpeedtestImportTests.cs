using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Speedtests;

public sealed class SpeedtestImportTests : IDisposable
{
    private static readonly JsonSerializerOptions _apiJson = new(JsonSerializerDefaults.Web);
    private static readonly DateTime _morning = new(2026, 9, 16, 8, 5, 12, 345);

    private readonly List<TestDatabase> _databases = [];

    [Fact]
    public async Task ImportingAnExportIntoTheSameDatabaseAddsNothingAndKeepsEveryResult()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var database = NewDatabase();
        await SeedAsync(database, cancellationToken);
        var export = await ExportAsync(database, cancellationToken);

        await using var db = database.NewContext();
        var imported = await new SpeedtestRepository(db).ImportTestsAsync(Parse(export), cancellationToken);

        Assert.Equal(0, imported);
        Assert.Equal(3, await new SpeedtestRepository(db).CountAsync(cancellationToken));
    }

    [Fact]
    public async Task ImportingIntoAnotherDatabaseWhoseIdsOverlapRestoresEveryField()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedAsync(source, cancellationToken);
        var export = await ExportAsync(source, cancellationToken);

        var target = NewDatabase();
        await using (var db = target.NewContext())
        {
            await new SpeedtestRepository(db).CreateAsync(new Speedtest { Download = 1, Created = _morning.AddDays(-30) }, cancellationToken);
        }

        await using (var db = target.NewContext())
        {
            Assert.Equal(3, await new SpeedtestRepository(db).ImportTestsAsync(Parse(export), cancellationToken));
        }

        await using var check = target.NewContext();
        var restored = await check.Speedtests.OrderBy(t => t.Created).ToListAsync(cancellationToken);
        Assert.Equal(4, restored.Count);

        var healthy = restored.Single(t => t.Status == TestStatus.Completed && t.Download > 1);
        Assert.Equal(_morning, healthy.Created);
        Assert.Equal((12, 941.25, 110.5, 0.75), (healthy.Ping, healthy.Download, healthy.Upload, healthy.Jitter!.Value));
        Assert.Equal((true, 25, 100.0, 50.0), (healthy.Healthy!.Value, healthy.ThresholdPing!.Value, healthy.ThresholdDownload!.Value, healthy.ThresholdUpload!.Value));
        Assert.Equal((TestType.Custom, "Acme Fibre", "speed.acme.example", "r-1"), (healthy.Type, healthy.ServerName, healthy.ServerHost, healthy.ResultId));

        Assert.Equal("Network unreachable", restored.Single(t => t.Status == TestStatus.Failed).Error);
        Assert.Equal("Public IP 203.0.113.9 is on the skip list", restored.Single(t => t.Status == TestStatus.Skipped).Error);
    }

    [Fact]
    public async Task ImportingTheSameFileTwiceAddsItsResultsOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedAsync(source, cancellationToken);
        var export = await ExportAsync(source, cancellationToken);
        var target = NewDatabase();

        await using (var db = target.NewContext())
        {
            Assert.Equal(3, await new SpeedtestRepository(db).ImportTestsAsync(Parse(export), cancellationToken));
        }

        await using (var db = target.NewContext())
        {
            Assert.Equal(0, await new SpeedtestRepository(db).ImportTestsAsync(Parse(export), cancellationToken));
            Assert.Equal(3, await new SpeedtestRepository(db).CountAsync(cancellationToken));
        }
    }

    [Fact]
    public async Task AResultListedTwiceInOneFileIsImportedOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedAsync(source, cancellationToken);
        var tests = Parse(await ExportAsync(source, cancellationToken));
        var target = NewDatabase();

        await using var db = target.NewContext();
        var imported = await new SpeedtestRepository(db).ImportTestsAsync(tests.Concat(Parse(SpeedtestExport.ToJson(tests))), cancellationToken);

        Assert.Equal(3, imported);
    }

    [Fact]
    public async Task ExportsWithPascalCaseNamesFromEarlierVersionsStillImport()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedAsync(source, cancellationToken);
        await using var sourceDb = source.NewContext();
        var pascalCaseExport = JsonSerializer.Serialize(await new SpeedtestRepository(sourceDb).ListAllAsync(cancellationToken));
        var target = NewDatabase();

        await using var db = target.NewContext();
        var imported = await new SpeedtestRepository(db).ImportTestsAsync(Parse(pascalCaseExport), cancellationToken);

        Assert.Equal(3, imported);
        Assert.Equal(1, await db.Speedtests.CountAsync(t => t.Status == TestStatus.Skipped, cancellationToken));
    }

    [Theory]
    [InlineData("paused", null, "cron", TestStatus.Completed, TestType.Auto)]
    [InlineData(null, "Network unreachable", null, TestStatus.Failed, TestType.Auto)]
    [InlineData("SKIPPED", "On the skip list", "Custom", TestStatus.Skipped, TestType.Custom)]
    public void StatusesAndTypesItDoesNotKnowFallBackToWhatTheRowSuggests(
        string? status, string? error, string? type, TestStatus expectedStatus, TestType expectedType)
    {
        var row = new SpeedtestImportRow { Status = status, Error = error, Type = type }.ToSpeedtest();

        Assert.Equal((expectedStatus, expectedType), (row.Status, row.Type));
    }

    private static List<Speedtest> Parse(string json) =>
        JsonSerializer.Deserialize<List<SpeedtestImportRow>>(json, _apiJson)!.Select(row => row.ToSpeedtest()).ToList();

    private static async Task<string> ExportAsync(TestDatabase database, CancellationToken cancellationToken)
    {
        await using var db = database.NewContext();
        return SpeedtestExport.ToJson(await new SpeedtestRepository(db).ListAllAsync(cancellationToken));
    }

    private static async Task SeedAsync(TestDatabase database, CancellationToken cancellationToken)
    {
        await using var db = database.NewContext();
        var repo = new SpeedtestRepository(db);
        await repo.CreateAsync(new Speedtest
        {
            Ping = 12, Jitter = 0.75, Download = 941.25, Upload = 110.5, Healthy = true,
            ThresholdPing = 25, ThresholdDownload = 100, ThresholdUpload = 50,
            Type = TestType.Custom, ServerName = "Acme Fibre", ServerHost = "speed.acme.example", ResultId = "r-1", Time = 14,
            Created = _morning
        }, cancellationToken);
        await repo.CreateAsync(new Speedtest { Ping = -1, Download = -1, Upload = -1, Status = TestStatus.Failed, Error = "Network unreachable", Created = _morning.AddHours(1) }, cancellationToken);
        await repo.CreateAsync(new Speedtest { Ping = -1, Download = -1, Upload = -1, Status = TestStatus.Skipped, Error = "Public IP 203.0.113.9 is on the skip list", Created = _morning.AddHours(2) }, cancellationToken);
    }

    private TestDatabase NewDatabase()
    {
        var database = new TestDatabase();
        _databases.Add(database);
        return database;
    }

    public void Dispose()
    {
        foreach (var database in _databases) database.Dispose();
    }
}
