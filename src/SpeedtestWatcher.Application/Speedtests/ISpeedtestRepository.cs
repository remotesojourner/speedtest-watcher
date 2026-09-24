namespace SpeedtestWatcher.Application.Speedtests;

public interface ISpeedtestRepository
{
    Task<int> CreateAsync(Speedtest test, CancellationToken cancellationToken = default);
    Task<Speedtest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Speedtest?> GetLatestAsync(CancellationToken cancellationToken = default);

    Task<Speedtest?> GetLatestCompletedAsync(CancellationToken cancellationToken = default);
    Task<List<Speedtest>> ListTestsAsync(int? afterId, int limit, TestStatus? status = null, TestType? type = null, bool? healthy = null, CancellationToken cancellationToken = default);
    Task<List<Speedtest>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<List<Speedtest>> ListMatchingAsync(TestStatus? status, TestType? type, bool? healthy, IReadOnlyCollection<int>? ids = null, CancellationToken cancellationToken = default);

    Task<int> CountMatchingAsync(TestStatus? status, TestType? type, bool? healthy, CancellationToken cancellationToken = default);
    Task<List<Speedtest>> ListCreatedBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default);
    Task<int> RemoveOldTestsAsync(int retentionDays, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<long> SumBytesSinceAsync(DateTime? sinceUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<long>> RecentRunBytesAsync(int count, CancellationToken cancellationToken = default);
}
