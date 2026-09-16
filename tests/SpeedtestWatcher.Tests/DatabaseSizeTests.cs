using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Data;
using SpeedtestWatcher.Infrastructure.Repositories;

namespace SpeedtestWatcher.Tests;

public class DatabaseSizeTests : IDisposable
{
    private const int ResultCount = 2000;

    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"speedtest-watcher-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task DatabaseSize_CountsResultsThatAreStillInTheWriteAheadLog()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(_folder);
        var databasePath = Path.Combine(_folder, "storage.db");

        await using var db = new SpeedtestWatcherDbContext(
            new DbContextOptionsBuilder<SpeedtestWatcherDbContext>().UseSqlite($"Data Source={databasePath}").Options);
        await db.Database.OpenConnectionAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken);

        var created = new DateTime(2026, 9, 1, 0, 0, 0);
        db.Speedtests.AddRange(Enumerable.Range(0, ResultCount).Select(i => new Speedtest
        {
            Download = 900 + i % 50,
            Upload = 100,
            Ping = 10,
            ServerName = $"Speed test server {i} operated by a provider with a fairly long descriptive name",
            Created = created.AddHours(i)
        }));
        await db.SaveChangesAsync(cancellationToken);

        var mainFileBytes = new FileInfo(databasePath).Length;
        var writeAheadLogBytes = new FileInfo(databasePath + "-wal").Length;
        var reported = await new StorageRepository(db).GetDatabaseSizeAsync(cancellationToken);

        Assert.True(writeAheadLogBytes > mainFileBytes, $"expected the results to still be in the write-ahead log ({writeAheadLogBytes} vs {mainFileBytes} bytes)");
        Assert.True(reported > mainFileBytes * 2, $"reported {reported} bytes, but the main file alone is {mainFileBytes} bytes");
        Assert.True(reported >= ResultCount * 100, $"reported {reported} bytes for {ResultCount} results");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }
}
