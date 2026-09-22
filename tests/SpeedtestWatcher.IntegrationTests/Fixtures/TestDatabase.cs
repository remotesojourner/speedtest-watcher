using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.IntegrationTests.Fixtures;

internal sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDatabase() : this(createSchema: true)
    {
    }

    private TestDatabase(bool createSchema)
    {
        _connection.Open();
        if (!createSchema) return;

        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public static TestDatabase WithoutSchema() => new(createSchema: false);

    public SpeedtestWatcherDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<SpeedtestWatcherDbContext>();
        Configure(options);
        return new SpeedtestWatcherDbContext(options.Options);
    }

    public void Configure(DbContextOptionsBuilder options) =>
        options.UseSqlite(_connection).ConfigureWarnings(warnings => warnings.Throw(
            CoreEventId.FirstWithoutOrderByAndFilterWarning,
            CoreEventId.RowLimitingOperationWithoutOrderByWarning));

    public void Dispose() => _connection.Dispose();
}
