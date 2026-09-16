using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Infrastructure.Data;

namespace SpeedtestWatcher.Infrastructure.Repositories;

public class StorageRepository : IStorageRepository
{
    private const string LogicalSizeQuery =
        "SELECT (SELECT page_count FROM pragma_page_count()) * (SELECT page_size FROM pragma_page_size()) AS Value";

    private readonly SpeedtestWatcherDbContext _db;

    public StorageRepository(SpeedtestWatcherDbContext db)
    {
        _db = db;
    }

    public async Task<long> GetDatabaseSizeAsync(CancellationToken cancellationToken = default)
    {
        var sizes = await _db.Database.SqlQueryRaw<long>(LogicalSizeQuery).ToListAsync(cancellationToken);
        return sizes.Single();
    }
}
