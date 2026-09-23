using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Resources;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Recommendations;

public sealed class RecommendationService
{
    public const int CompletedTestsNeeded = 10;

    private const int PlaceholderPing = 25;
    private const double PlaceholderDownload = 100;
    private const double PlaceholderUpload = 50;

    private readonly IRecommendationRepository _recommendations;
    private readonly ISpeedtestRepository _results;

    public RecommendationService(IRecommendationRepository recommendations, ISpeedtestRepository results)
    {
        _recommendations = recommendations;
        _results = results;
    }

    public async Task<OperationResult<Recommendation>> GetAsync(CancellationToken cancellationToken = default) =>
        await _recommendations.GetAsync(cancellationToken) is { } recommendation
            ? OperationResult.Ok(recommendation)
            : OperationResult.NotFound(ApplicationStrings.RecommendationsNotFound);

    public async Task<Recommendation?> RecalculateAsync(CancellationToken cancellationToken = default)
    {
        var recent = await _results.ListTestsAsync(null, CompletedTestsNeeded, TestStatus.Completed, cancellationToken: cancellationToken);
        if (recent.Count < CompletedTestsNeeded) return null;

        var ping = recent.Min(test => test.Ping);
        var download = Math.Round(recent.Max(test => test.Download), 2);
        var upload = Math.Round(recent.Max(test => test.Upload), 2);

        if (await _recommendations.GetAsync(cancellationToken) is { } current
            && current.Ping == ping && current.Download == download && current.Upload == upload)
            return null;

        return await _recommendations.SaveAsync(ping, download, upload, cancellationToken);
    }

    public async Task RemovePlaceholderAsync(CancellationToken cancellationToken = default)
    {
        if (await _results.CountMatchingAsync(TestStatus.Completed, null, null, cancellationToken) >= CompletedTestsNeeded) return;

        if (await _recommendations.GetAsync(cancellationToken) is { Ping: PlaceholderPing, Download: PlaceholderDownload, Upload: PlaceholderUpload })
            await _recommendations.ClearAllAsync(cancellationToken);
    }
}
