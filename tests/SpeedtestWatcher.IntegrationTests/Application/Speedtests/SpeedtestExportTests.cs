using System.Text.Json;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Speedtests;

public class SpeedtestExportTests : IDisposable
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private readonly TestDatabase _database = new();
    private readonly SpeedtestWatcherDbContext _db;

    public SpeedtestExportTests()
    {
        _db = _database.NewContext();
    }

    [Fact]
    public void Csv_QuotesProviderText_AndDefusesSpreadsheetFormulas()
    {
        var csv = SpeedtestExport.ToCsv(
        [
            new Speedtest
            {
                Id = 7,
                Created = new DateTime(2026, 9, 16, 8, 5, 0),
                Ping = 13,
                Jitter = 1.4,
                Download = 941.2,
                Upload = 109.8,
                ServerName = "Acme, Inc",
                Error = "=HYPERLINK(\"http://example.com\")",
                Healthy = null
            }
        ]);

        var lines = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(SpeedtestExport.CsvHeader, lines[0]);
        Assert.StartsWith("7,2026-09-16T08:05:00Z,completed,,auto,13,1.4,941.2,109.8,", lines[1]);
        Assert.Contains(",\"Acme, Inc\",", lines[1]);
        Assert.EndsWith(",\"'=HYPERLINK(\"\"http://example.com\"\")\"", lines[1]);
    }

    [Fact]
    public void Json_RoundTripsIntoTheShapeStorageImports()
    {
        var json = SpeedtestExport.ToJson([new Speedtest { Id = 3, Download = 100, Status = TestStatus.Skipped, Error = "on the skip list" }]);

        var parsed = JsonSerializer.Deserialize<List<SpeedtestImportRow>>(json, WebJson)!;
        Assert.Equal("on the skip list", Assert.Single(parsed).Error);
        Assert.Equal("skipped", parsed[0].Status);
    }

    [Fact]
    public async Task Repository_ListsAndCountsWhatTheFiltersMatch_OrOnlyTheGivenIds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var repo = new SpeedtestRepository(_db);
        var now = DateTime.UtcNow;
        var completed = await repo.CreateAsync(new Speedtest { Download = 100, Created = now.AddMinutes(-3) }, cancellationToken);
        var missed = await repo.CreateAsync(new Speedtest { Download = 5, Healthy = false, Type = TestType.Custom, Created = now.AddMinutes(-2) }, cancellationToken);
        await repo.CreateAsync(new Speedtest { Status = TestStatus.Skipped, Error = "on the skip list", Created = now.AddMinutes(-1) }, cancellationToken);

        Assert.Equal(3, await repo.CountMatchingAsync(null, null, null, cancellationToken));
        Assert.Equal(2, await repo.CountMatchingAsync(TestStatus.Completed, null, null, cancellationToken));
        Assert.Equal(1, await repo.CountMatchingAsync(null, TestType.Custom, false, cancellationToken));

        var both = await repo.ListMatchingAsync(TestStatus.Completed, null, null, cancellationToken: cancellationToken);
        Assert.Equal([missed, completed], both.Select(t => t.Id));

        var chosen = await repo.ListMatchingAsync(null, null, null, [completed], cancellationToken);
        Assert.Equal(completed, Assert.Single(chosen).Id);
    }

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }
}
