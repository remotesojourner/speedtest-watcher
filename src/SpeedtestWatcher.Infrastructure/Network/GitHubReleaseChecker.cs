using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Core.Hosting;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Infrastructure.Network;

public class GitHubReleaseChecker : IReleaseChecker
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitHubReleaseChecker> _logger;

    public GitHubReleaseChecker(IHttpClientFactory httpClientFactory, ILogger<GitHubReleaseChecker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> GetLatestVersionAsync(CancellationToken cancellationToken = default)
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
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Could not check {Repository} for a newer release", ProjectInfo.Repository);
            return null;
        }
    }
}
