using SpeedtestWatcher.Application.Resources;
using System.Text;
using Cronos;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Application.Speedtests;

public sealed class ResultsService
{
    private readonly ISpeedtestRepository _results;
    private readonly ICurrentAccess _access;

    private const int RunsBehindAnEstimate = 10;

    public ResultsService(ISpeedtestRepository results, ICurrentAccess access)
    {
        _results = results;
        _access = access;
    }

    public async Task<IReadOnlyList<SpeedtestDto>> ListAsync(
        int? afterId, int limit, TestStatus? status, TestType? type, bool? healthy, CancellationToken cancellationToken = default) =>
        (await _results.ListTestsAsync(afterId, limit, status, type, healthy, cancellationToken)).Select(Visible).ToList();

    public async Task<OperationResult<SpeedtestDto>> GetAsync(int id, CancellationToken cancellationToken = default) =>
        await _results.GetByIdAsync(id, cancellationToken) is { } test
            ? OperationResult.Ok(Visible(test))
            : OperationResult.NotFound(ApplicationStrings.SpeedtestNotFound);

    private SpeedtestDto Visible(Speedtest test)
    {
        var dto = SpeedtestDto.From(test);
        if (!_access.HasFullAccess) dto.PublicIp = null;
        return dto;
    }

    public Task<int> CountAsync(TestStatus? status, TestType? type, bool? healthy, CancellationToken cancellationToken = default) =>
        _results.CountMatchingAsync(status, type, healthy, cancellationToken);

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        return await _results.DeleteByIdAsync(id, cancellationToken)
            ? OperationResult.Ok()
            : OperationResult.NotFound(ApplicationStrings.SpeedtestNotFound);
    }

    public async Task<ExportFile> ExportAsync(ExportRequest request, CancellationToken cancellationToken = default) =>
        Export(await _results.ListMatchingAsync(request.Status, request.Type, request.Healthy, request.Ids, cancellationToken), request.Format);

    public async Task<OperationResult<TestImportResultDto>> ImportAsync(IReadOnlyList<SpeedtestImportRow>? rows, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (rows == null || rows.Count == 0) return OperationResult.Invalid(ApplicationStrings.NoTestsProvided);

        var imported = await _results.ImportTestsAsync(rows.Select(row => row.ToSpeedtest()), cancellationToken);
        return OperationResult.Ok(new TestImportResultDto { Imported = imported, Skipped = rows.Count - imported });
    }

    public Task<Speedtest?> GetLatestCompletedAsync(CancellationToken cancellationToken = default) =>
        _results.GetLatestCompletedAsync(cancellationToken);

    public async Task<DataUsedDto> DataUsedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return new DataUsedDto(
            await _results.SumBytesSinceAsync(now.AddHours(-24), cancellationToken),
            await _results.SumBytesSinceAsync(now.AddDays(-7), cancellationToken),
            await _results.SumBytesSinceAsync(now.AddDays(-30), cancellationToken),
            await _results.SumBytesSinceAsync(null, cancellationToken));
    }

    public async Task<long?> EstimateBytesAsync(ScheduleSettings schedule, TimeSpan window, CancellationToken cancellationToken = default)
    {
        int runs;
        try
        {
            runs = schedule.RunsIn(window, DateTime.UtcNow);
        }
        catch (CronFormatException)
        {
            return null;
        }

        var recent = await _results.RecentRunBytesAsync(RunsBehindAnEstimate, cancellationToken);
        if (recent.Count == 0 || runs == 0) return null;

        var sorted = recent.Order().ToList();
        return sorted[sorted.Count / 2] * runs;
    }

    public async Task<ResultsSummary> SummarizeAsync(CancellationToken cancellationToken = default) => new(
        await _results.GetLatestAsync(cancellationToken),
        await _results.GetLatestCompletedAsync(cancellationToken),
        await _results.CountAsync(cancellationToken));

    public static ExportFile Export(IEnumerable<Speedtest> tests, string format) =>
        format.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? new ExportFile(Encoding.UTF8.GetBytes(SpeedtestExport.ToJson(tests)), "application/json", "speedtests.json")
            : new ExportFile(Encoding.UTF8.GetBytes(SpeedtestExport.ToCsv(tests)), "text/csv", "speedtests.csv");
}
