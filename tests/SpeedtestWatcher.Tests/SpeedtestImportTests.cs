using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.Tests;

public class SpeedtestImportTests : IDisposable
{
    private static readonly JsonSerializerOptions ApiJson = new(JsonSerializerDefaults.Web);
    private static readonly DateTime Morning = new(2026, 9, 16, 8, 5, 12, 345);

    private readonly List<SqliteConnection> _databases = [];

    [Fact]
    public async Task ImportingAnExportIntoTheSameDatabase_AddsNothing_AndKeepsEveryResult()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var database = NewDatabase();
        await SeedAsync(database, cancellationToken);
        var export = await ExportAsync(database, cancellationToken);

        await using var db = Context(database);
        var imported = await new SpeedtestRepository(db).ImportTestsAsync(Parse(export), cancellationToken);

        Assert.Equal(0, imported);
        Assert.Equal(3, await new SpeedtestRepository(db).CountAsync(cancellationToken));
    }

    [Fact]
    public async Task ImportingIntoAnotherDatabase_WhoseIdsOverlap_RestoresEveryField()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedAsync(source, cancellationToken);
        var export = await ExportAsync(source, cancellationToken);

        var target = NewDatabase();
        await using (var db = Context(target))
        {
            await new SpeedtestRepository(db).CreateAsync(new Speedtest { Download = 1, Created = Morning.AddDays(-30) }, cancellationToken);
        }

        await using (var db = Context(target))
        {
            Assert.Equal(3, await new SpeedtestRepository(db).ImportTestsAsync(Parse(export), cancellationToken));
        }

        await using var check = Context(target);
        var restored = await check.Speedtests.OrderBy(t => t.Created).ToListAsync(cancellationToken);
        Assert.Equal(4, restored.Count);

        var healthy = restored.Single(t => t.Status == TestStatus.Completed && t.Download > 1);
        Assert.Equal(Morning, healthy.Created);
        Assert.Equal((12, 941.25, 110.5, 0.75), (healthy.Ping, healthy.Download, healthy.Upload, healthy.Jitter!.Value));
        Assert.Equal((true, 25, 100.0, 50.0), (healthy.Healthy!.Value, healthy.ThresholdPing!.Value, healthy.ThresholdDownload!.Value, healthy.ThresholdUpload!.Value));
        Assert.Equal((TestType.Custom, "Acme Fibre", "speed.acme.example", "r-1"), (healthy.Type, healthy.ServerName, healthy.ServerHost, healthy.ResultId));

        Assert.Equal("Network unreachable", restored.Single(t => t.Status == TestStatus.Failed).Error);
        Assert.Equal("Public IP 203.0.113.9 is on the skip list", restored.Single(t => t.Status == TestStatus.Skipped).Error);
    }

    [Fact]
    public async Task ImportingTheSameFileTwice_AddsItsResultsOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedAsync(source, cancellationToken);
        var export = await ExportAsync(source, cancellationToken);
        var target = NewDatabase();

        await using (var db = Context(target))
        {
            Assert.Equal(3, await new SpeedtestRepository(db).ImportTestsAsync(Parse(export), cancellationToken));
        }

        await using (var db = Context(target))
        {
            Assert.Equal(0, await new SpeedtestRepository(db).ImportTestsAsync(Parse(export), cancellationToken));
            Assert.Equal(3, await new SpeedtestRepository(db).CountAsync(cancellationToken));
        }
    }

    [Fact]
    public async Task AResultListedTwiceInOneFile_IsImportedOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedAsync(source, cancellationToken);
        var tests = Parse(await ExportAsync(source, cancellationToken));
        var target = NewDatabase();

        await using var db = Context(target);
        var imported = await new SpeedtestRepository(db).ImportTestsAsync(tests.Concat(Parse(SpeedtestExport.ToJson(tests))), cancellationToken);

        Assert.Equal(3, imported);
    }

    [Fact]
    public async Task ExportsWithPascalCaseNames_FromEarlierVersions_StillImport()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedAsync(source, cancellationToken);
        await using var sourceDb = Context(source);
        var pascalCaseExport = JsonSerializer.Serialize(await new SpeedtestRepository(sourceDb).ListAllAsync(cancellationToken));
        var target = NewDatabase();

        await using var db = Context(target);
        var imported = await new SpeedtestRepository(db).ImportTestsAsync(Parse(pascalCaseExport), cancellationToken);

        Assert.Equal(3, imported);
        Assert.Equal(1, await db.Speedtests.CountAsync(t => t.Status == TestStatus.Skipped, cancellationToken));
    }

    [Theory]
    [InlineData("paused", null, "cron", TestStatus.Completed, TestType.Auto)]
    [InlineData(null, "Network unreachable", null, TestStatus.Failed, TestType.Auto)]
    [InlineData("SKIPPED", "On the skip list", "Custom", TestStatus.Skipped, TestType.Custom)]
    public void StatusesAndTypesItDoesNotKnow_FallBackToWhatTheRowSuggests(
        string? status, string? error, string? type, TestStatus expectedStatus, TestType expectedType)
    {
        var row = new SpeedtestImportRow { Status = status, Error = error, Type = type }.ToSpeedtest();

        Assert.Equal((expectedStatus, expectedType), (row.Status, row.Type));
    }

    private static List<Speedtest> Parse(string json) =>
        JsonSerializer.Deserialize<List<SpeedtestImportRow>>(json, ApiJson)!.Select(row => row.ToSpeedtest()).ToList();

    private static async Task<string> ExportAsync(SqliteConnection database, CancellationToken cancellationToken)
    {
        await using var db = Context(database);
        return SpeedtestExport.ToJson(await new SpeedtestRepository(db).ListAllAsync(cancellationToken));
    }

    private static async Task SeedAsync(SqliteConnection database, CancellationToken cancellationToken)
    {
        await using var db = Context(database);
        var repo = new SpeedtestRepository(db);
        await repo.CreateAsync(new Speedtest
        {
            Ping = 12, Jitter = 0.75, Download = 941.25, Upload = 110.5, Healthy = true,
            ThresholdPing = 25, ThresholdDownload = 100, ThresholdUpload = 50,
            Type = TestType.Custom, ServerName = "Acme Fibre", ServerHost = "speed.acme.example", ResultId = "r-1", Time = 14,
            Created = Morning
        }, cancellationToken);
        await repo.CreateAsync(new Speedtest { Ping = -1, Download = -1, Upload = -1, Status = TestStatus.Failed, Error = "Network unreachable", Created = Morning.AddHours(1) }, cancellationToken);
        await repo.CreateAsync(new Speedtest { Ping = -1, Download = -1, Upload = -1, Status = TestStatus.Skipped, Error = "Public IP 203.0.113.9 is on the skip list", Created = Morning.AddHours(2) }, cancellationToken);
    }

    private SqliteConnection NewDatabase()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        _databases.Add(connection);
        using var db = Context(connection);
        db.Database.EnsureCreated();
        return connection;
    }

    private static SpeedtestWatcherDbContext Context(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<SpeedtestWatcherDbContext>().UseSqlite(connection).Options);

    public void Dispose()
    {
        foreach (var connection in _databases) connection.Dispose();
    }
}
