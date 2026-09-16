using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Core.Helpers;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Data;
using SpeedtestWatcher.Infrastructure.Repositories;

namespace SpeedtestWatcher.Tests;

public class SpeedtestExportTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SpeedtestWatcherDbContext _db;

    public SpeedtestExportTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new SpeedtestWatcherDbContext(new DbContextOptionsBuilder<SpeedtestWatcherDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
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
        // The formula keeps its text but gains a leading quote, and its own quotes are doubled.
        Assert.EndsWith(",\"'=HYPERLINK(\"\"http://example.com\"\")\"", lines[1]);
    }

    [Fact]
    public void Json_RoundTripsIntoTheShapeStorageImports()
    {
        var json = SpeedtestExport.ToJson([new Speedtest { Id = 3, Download = 100, Status = "skipped", Error = "on the skip list" }]);

        var parsed = JsonSerializer.Deserialize<List<Speedtest>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(3, Assert.Single(parsed).Id);
        Assert.Equal("skipped", parsed[0].Status);
    }

    [Fact]
    public async Task Repository_ListsAndCountsWhatTheFiltersMatch_OrOnlyTheGivenIds()
    {
        var repo = new SpeedtestRepository(_db);
        var now = DateTime.UtcNow;
        int completed = await repo.CreateAsync(new Speedtest { Download = 100, Created = now.AddMinutes(-3) });
        int missed = await repo.CreateAsync(new Speedtest { Download = 5, Healthy = false, Type = "custom", Created = now.AddMinutes(-2) });
        await repo.CreateAsync(new Speedtest { Status = "skipped", Error = "on the skip list", Created = now.AddMinutes(-1) });

        Assert.Equal(3, await repo.CountMatchingAsync(null, null, null));
        Assert.Equal(2, await repo.CountMatchingAsync("completed", null, null));
        Assert.Equal(1, await repo.CountMatchingAsync(null, "custom", false));

        var both = await repo.ListMatchingAsync("completed", null, null);
        Assert.Equal([missed, completed], both.Select(t => t.Id));

        var chosen = await repo.ListMatchingAsync(null, null, null, [completed]);
        Assert.Equal(completed, Assert.Single(chosen).Id);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
