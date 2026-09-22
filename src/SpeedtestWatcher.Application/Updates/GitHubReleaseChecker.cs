using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Updates;

internal sealed class GitHubReleaseChecker : IReleaseChecker, IDisposable
{
    public static readonly TimeSpan AnswerLifetime = TimeSpan.FromHours(6);
    public static readonly TimeSpan FailureLifetime = TimeSpan.FromHours(1);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _time;
    private readonly ILogger<GitHubReleaseChecker> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _latestVersion;
    private DateTimeOffset _askAgainAt = DateTimeOffset.MinValue;

    public GitHubReleaseChecker(IHttpClientFactory httpClientFactory, TimeProvider time, ILogger<GitHubReleaseChecker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _time = time;
        _logger = logger;
    }

    public async Task<string?> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_time.GetUtcNow() < _askAgainAt) return _latestVersion;

            var answer = await AskGitHubAsync(cancellationToken);
            if (answer != null) _latestVersion = answer;
            _askAgainAt = _time.GetUtcNow() + (answer == null ? FailureLifetime : AnswerLifetime);
            return _latestVersion;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();

    private async Task<string?> AskGitHubAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("User-Agent", "SpeedtestWatcher");

            using var response = await client.GetAsync($"https://api.github.com/repos/{ProjectInfo.Repository}/releases/latest", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub answered {Status} when checking {Repository} for a newer release", (int)response.StatusCode, ProjectInfo.Repository);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return document.RootElement.TryGetProperty("tag_name", out var tag) ? tag.GetString()?.Replace("v", "", StringComparison.Ordinal) : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(ex, "Could not check {Repository} for a newer release", ProjectInfo.Repository);
            return null;
        }
    }
}
