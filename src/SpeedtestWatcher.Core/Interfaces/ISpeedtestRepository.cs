using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Core.Interfaces;

public interface ISpeedtestRepository
{
    Task<int> CreateAsync(Speedtest test, CancellationToken cancellationToken = default);
    Task<Speedtest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Speedtest?> GetLatestAsync(CancellationToken cancellationToken = default);

    Task<Speedtest?> GetLatestCompletedAsync(CancellationToken cancellationToken = default);
    Task<List<Speedtest>> ListTestsAsync(int? afterId, int limit, string? status = null, string? type = null, bool? healthy = null, CancellationToken cancellationToken = default);
    Task<List<Speedtest>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<List<Speedtest>> ListMatchingAsync(string? status, string? type, bool? healthy, IReadOnlyCollection<int>? ids = null, CancellationToken cancellationToken = default);

    Task<int> CountMatchingAsync(string? status, string? type, bool? healthy, CancellationToken cancellationToken = default);
    Task<StatisticsDto> GetStatisticsAsync(string fromDate, string toDate, CancellationToken cancellationToken = default);
    Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default);
    Task<int> ImportTestsAsync(IEnumerable<Speedtest> tests, CancellationToken cancellationToken = default);
    Task<int> RemoveOldTestsAsync(int retentionDays, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
