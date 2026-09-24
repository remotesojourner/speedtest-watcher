using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.TestSupport;

namespace SpeedtestWatcher.IntegrationTests.Application.Storage;

public sealed class DataBackupTests : IDisposable
{
    private static readonly DateTime _morning = new(2026, 9, 16, 8, 5, 12, 345);

    private readonly List<TestDatabase> _databases = [];

    [Fact]
    public async Task ExportingEveryTableAndImportingIntoAnEmptyDatabaseRestoresEveryRow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedEveryTableAsync(source, cancellationToken);

        var backup = await ExportAsync(source, cancellationToken);
        var target = NewDatabase();
        await using (var db = target.NewContext())
        {
            var result = (await Backup(db).ImportAsync(backup, cancellationToken)).Value!;
            Assert.Equal((3, 2, 2, 3, 0), (result.Speedtests, result.Outages, result.WatchSessions, result.ProbeRounds, result.Skipped));
        }

        await using var sourceDb = source.NewContext();
        await using var targetDb = target.NewContext();
        var sourceTests = await sourceDb.Speedtests.OrderBy(test => test.Created).ToListAsync(cancellationToken);
        var targetTests = await targetDb.Speedtests.OrderBy(test => test.Created).ToListAsync(cancellationToken);
        Assert.Equal(sourceTests.Count, targetTests.Count);
        foreach (var (expected, actual) in sourceTests.Zip(targetTests)) AssertSameSpeedtest(expected, actual);

        var sourceOutages = await sourceDb.Outages.ToListAsync(cancellationToken);
        var sourceSessions = await sourceDb.WatchSessions.ToListAsync(cancellationToken);
        var targetOutages = await targetDb.Outages.ToListAsync(cancellationToken);
        var targetSessions = await targetDb.WatchSessions.ToListAsync(cancellationToken);

        var window = (From: _morning.AddDays(-2), To: _morning.AddDays(2));
        var sourceUptime = Uptime.Over(window.From, window.To, sourceSessions, sourceOutages, window.To);
        var targetUptime = Uptime.Over(window.From, window.To, targetSessions, targetOutages, window.To);
        Assert.Equal(sourceUptime, targetUptime);

        Assert.Equal(await sourceDb.ProbeRounds.CountAsync(cancellationToken), await targetDb.ProbeRounds.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task ImportingIntoADatabaseWhoseIdsOverlapKeepsBothSets()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedEveryTableAsync(source, cancellationToken);
        var backup = await ExportAsync(source, cancellationToken);

        var target = NewDatabase();
        await using (var db = target.NewContext())
        {
            await new SpeedtestRepository(db).CreateAsync(FullSpeedtest(_morning.AddDays(-30)), cancellationToken);
            db.Outages.Add(new Outage { StartedAt = _morning.AddDays(-30), EndedAt = _morning.AddDays(-30).AddMinutes(5) });
            db.WatchSessions.Add(new WatchSession { StartedAt = _morning.AddDays(-30), LastSeenAt = _morning.AddDays(-30).AddHours(1) });
            db.ProbeRounds.Add(new ProbeRound { At = _morning.AddDays(-30), Passed = true, Answered = 1, Asked = 1 });
            await db.SaveChangesAsync(cancellationToken);
        }

        await using (var db = target.NewContext())
        {
            var result = (await Backup(db).ImportAsync(backup, cancellationToken)).Value!;
            Assert.Equal((3, 2, 2, 3, 0), (result.Speedtests, result.Outages, result.WatchSessions, result.ProbeRounds, result.Skipped));
        }

        await using var check = target.NewContext();
        Assert.Equal(4, await check.Speedtests.CountAsync(cancellationToken));
        Assert.Equal(3, await check.Outages.CountAsync(cancellationToken));
        Assert.Equal(3, await check.WatchSessions.CountAsync(cancellationToken));
        Assert.Equal(4, await check.ProbeRounds.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task ImportingTheSameFileTwiceAddsNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedEveryTableAsync(source, cancellationToken);
        var backup = await ExportAsync(source, cancellationToken);
        var target = NewDatabase();

        await using (var db = target.NewContext()) await Backup(db).ImportAsync(backup, cancellationToken);

        await using (var db = target.NewContext())
        {
            var result = (await Backup(db).ImportAsync(backup, cancellationToken)).Value!;
            Assert.Equal((0, 0, 0, 0), (result.Speedtests, result.Outages, result.WatchSessions, result.ProbeRounds));
            Assert.Equal(3 + 2 + 2 + 3, result.Skipped);
        }
    }

    [Fact]
    public async Task ABareArrayFromAResultsExportImportsAsResultsOnly()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedEveryTableAsync(source, cancellationToken);

        await using var sourceDb = source.NewContext();
        var resultsOnly = SpeedtestExport.ToJson(await new SpeedtestRepository(sourceDb).ListAllAsync(cancellationToken));
        var backup = DataBackupFile.Parse(resultsOnly)!;

        var target = NewDatabase();
        await using var db = target.NewContext();
        var result = (await Backup(db).ImportAsync(backup, cancellationToken)).Value!;

        Assert.Equal((3, 0, 0, 0), (result.Speedtests, result.Outages, result.WatchSessions, result.ProbeRounds));
    }

    [Fact]
    public async Task ExportsWithPascalCaseNamesFromEarlierVersionsStillImport()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var source = NewDatabase();
        await SeedEveryTableAsync(source, cancellationToken);
        await using var sourceDb = source.NewContext();
        var pascalCaseExport = JsonSerializer.Serialize(await new SpeedtestRepository(sourceDb).ListAllAsync(cancellationToken));
        var backup = DataBackupFile.Parse(pascalCaseExport)!;

        var target = NewDatabase();
        await using var db = target.NewContext();
        var result = (await Backup(db).ImportAsync(backup, cancellationToken)).Value!;

        Assert.Equal(3, result.Speedtests);
        Assert.Equal(1, await db.Speedtests.CountAsync(test => test.Status == TestStatus.Skipped, cancellationToken));
    }

    [Theory]
    [InlineData("paused", null, "cron", TestStatus.Completed, TestType.Auto)]
    [InlineData(null, "Network unreachable", null, TestStatus.Failed, TestType.Auto)]
    [InlineData("SKIPPED", "On the skip list", "Custom", TestStatus.Skipped, TestType.Custom)]
    public void StatusesAndTypesItDoesNotKnowFallBackToWhatTheRowSuggests(
        string? status, string? error, string? type, TestStatus expectedStatus, TestType expectedType)
    {
        var row = new SpeedtestImportRow { Status = status, Error = error, Type = type, Created = _morning }.ToSpeedtest();

        Assert.Equal((expectedStatus, expectedType), (row.Status, row.Type));
    }

    [Fact]
    public async Task AFileWithAResultMissingCreatedStoresNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var backup = new DataBackupDto { Speedtests = [new SpeedtestImportRow { Download = 100, Created = _morning }, new SpeedtestImportRow { Download = 200 }] };

        var target = NewDatabase();
        await using var db = target.NewContext();
        var result = await Backup(db).ImportAsync(backup, cancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationOutcome.Invalid, result.Outcome);
        Assert.Equal(0, await db.Speedtests.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task AnOpenOutageAndAReversedRangeAreSkippedAndCounted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var backup = new DataBackupDto
        {
            Outages =
            [
                new DataBackupOutageRow { StartedAt = _morning, EndedAt = _morning.AddMinutes(5) },
                new DataBackupOutageRow { StartedAt = _morning.AddHours(1), EndedAt = null },
                new DataBackupOutageRow { StartedAt = _morning.AddHours(2), EndedAt = _morning.AddHours(1) }
            ]
        };

        var target = NewDatabase();
        await using var db = target.NewContext();
        var result = (await Backup(db).ImportAsync(backup, cancellationToken)).Value!;

        Assert.Equal(1, result.Outages);
        Assert.Equal(2, result.Skipped);
        Assert.Equal(1, await db.Outages.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task AnUnknownVersionIsRejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var backup = new DataBackupDto
        {
            Version = DataBackupDto.CurrentVersion + 1,
            Speedtests = [new SpeedtestImportRow { Download = 100, Created = _morning }]
        };

        var target = NewDatabase();
        await using var db = target.NewContext();
        var result = await Backup(db).ImportAsync(backup, cancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationOutcome.Invalid, result.Outcome);
        Assert.Equal(0, await db.Speedtests.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task AReadOnlyVisitorIsDeniedAllOperations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var database = NewDatabase();
        await using var db = database.NewContext();
        var service = new DataBackupService(new DataBackupRepository(db), FixedAccess.ReadOnly);

        Assert.Equal(OperationOutcome.Denied, (await service.ExportAsync(cancellationToken)).Outcome);
        Assert.Equal(OperationOutcome.Denied, (await service.ImportAsync(new DataBackupDto { Speedtests = [new SpeedtestImportRow { Created = _morning }] }, cancellationToken)).Outcome);
        Assert.Equal(OperationOutcome.Denied, (await service.DeleteAllAsync(cancellationToken)).Outcome);
    }

    [Fact]
    public async Task DeletingAllDataEmptiesEveryTable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var database = NewDatabase();
        await SeedEveryTableAsync(database, cancellationToken);

        await using (var db = database.NewContext())
        {
            var result = await Backup(db).DeleteAllAsync(cancellationToken);
            Assert.True(result.Succeeded);
        }

        await using var check = database.NewContext();
        Assert.Equal(0, await check.Speedtests.CountAsync(cancellationToken));
        Assert.Equal(0, await check.Outages.CountAsync(cancellationToken));
        Assert.Equal(0, await check.WatchSessions.CountAsync(cancellationToken));
        Assert.Equal(0, await check.ProbeRounds.CountAsync(cancellationToken));
    }

    private static void AssertSameSpeedtest(Speedtest expected, Speedtest actual)
    {
        foreach (var property in typeof(Speedtest).GetProperties().Where(property => property.Name != nameof(Speedtest.Id)))
            Assert.Equal(property.GetValue(expected), property.GetValue(actual));
    }

    private static Speedtest FullSpeedtest(DateTime created) => new()
    {
        ServerId = 74367,
        ServerName = "BT",
        ServerHost = "speedtest3.networks.bt.com",
        Ping = 11,
        Jitter = 0.18,
        Download = 932.63,
        Upload = 110.07,
        PacketLoss = 0.5,
        BufferbloatDown = 5.1,
        BufferbloatUp = 42.8,
        LatencyIdle = 11.4,
        LatencyLoaded = 13.2,
        LatencyLoadedTail = 64.2,
        DownloadBytes = 903347628,
        UploadBytes = 88429797,
        PublicIp = "203.0.113.9",
        Status = TestStatus.Completed,
        Healthy = true,
        ThresholdPing = 25,
        ThresholdDownload = 900,
        ThresholdUpload = 100,
        ThresholdPacketLoss = 1,
        ThresholdBufferbloat = 50,
        Type = TestType.Auto,
        ResultId = "r-1",
        Time = 12,
        Created = created
    };

    private static async Task SeedEveryTableAsync(TestDatabase database, CancellationToken cancellationToken)
    {
        await using var db = database.NewContext();
        var repo = new SpeedtestRepository(db);
        await repo.CreateAsync(FullSpeedtest(_morning), cancellationToken);
        await repo.CreateAsync(new Speedtest { Ping = -1, Download = -1, Upload = -1, Status = TestStatus.Failed, Error = "Network unreachable", Created = _morning.AddHours(1) }, cancellationToken);
        await repo.CreateAsync(new Speedtest { Ping = -1, Download = -1, Upload = -1, Status = TestStatus.Skipped, Error = "Public IP 203.0.113.9 is on the skip list", Created = _morning.AddHours(2) }, cancellationToken);

        db.Outages.Add(new Outage { StartedAt = _morning.AddHours(-1), EndedAt = _morning.AddHours(-1).AddMinutes(5) });
        db.Outages.Add(new Outage { StartedAt = _morning.AddDays(1), EndedAt = _morning.AddDays(1).AddMinutes(10) });
        db.WatchSessions.Add(new WatchSession { StartedAt = _morning.AddHours(-2), LastSeenAt = _morning });
        db.WatchSessions.Add(new WatchSession { StartedAt = _morning.AddDays(1), LastSeenAt = _morning.AddDays(1).AddHours(1) });
        db.ProbeRounds.Add(new ProbeRound { At = _morning, Passed = true, Answered = 3, Asked = 3, FastestMilliseconds = 11.8, DuringTest = false });
        db.ProbeRounds.Add(new ProbeRound { At = _morning.AddMinutes(15), Passed = false, Answered = 0, Asked = 3, DuringTest = true });
        db.ProbeRounds.Add(new ProbeRound { At = _morning.AddMinutes(30), Passed = true, Answered = 2, Asked = 3, FastestMilliseconds = 14.2 });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<DataBackupDto> ExportAsync(TestDatabase database, CancellationToken cancellationToken)
    {
        await using var db = database.NewContext();
        var file = (await Backup(db).ExportAsync(cancellationToken)).Value!;
        return DataBackupFile.Parse(Encoding.UTF8.GetString(file.Content))!;
    }

    private static DataBackupService Backup(SpeedtestWatcherDbContext db) => new(new DataBackupRepository(db), FixedAccess.Full);

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
