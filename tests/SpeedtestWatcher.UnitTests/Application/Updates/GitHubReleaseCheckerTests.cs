using System.Net;
using Microsoft.Extensions.Time.Testing;
using SpeedtestWatcher.Application.Updates;
using SpeedtestWatcher.TestSupport;

namespace SpeedtestWatcher.UnitTests.Application.Updates;

public sealed class GitHubReleaseCheckerTests : IDisposable
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));
    private readonly RecordingHandler _handler = new() { ResponseBody = """{"tag_name":"v1.4.0"}""" };
    private readonly RecordingLogger<GitHubReleaseChecker> _logger = new();
    private readonly GitHubReleaseChecker _checker;

    public GitHubReleaseCheckerTests()
    {
        _checker = new GitHubReleaseChecker(new StubHttpClientFactory(_handler), _time, _logger);
    }

    [Fact]
    public async Task TheAnswerIsReusedForSixHours()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal("1.4.0", await _checker.GetLatestVersionAsync(cancellationToken));
        _time.Advance(GitHubReleaseChecker.AnswerLifetime - TimeSpan.FromMinutes(1));
        Assert.Equal("1.4.0", await _checker.GetLatestVersionAsync(cancellationToken));
        Assert.Single(_handler.Requests);

        _time.Advance(TimeSpan.FromMinutes(1));
        await _checker.GetLatestVersionAsync(cancellationToken);
        Assert.Equal(2, _handler.Requests.Count);
    }

    [Fact]
    public async Task ARefusalIsNotRetriedForAnHourAndTheLastKnownVersionIsKept()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _checker.GetLatestVersionAsync(cancellationToken);
        _time.Advance(GitHubReleaseChecker.AnswerLifetime);
        _handler.ResponseStatus = HttpStatusCode.Forbidden;

        Assert.Equal("1.4.0", await _checker.GetLatestVersionAsync(cancellationToken));
        _time.Advance(GitHubReleaseChecker.FailureLifetime - TimeSpan.FromMinutes(1));
        Assert.Equal("1.4.0", await _checker.GetLatestVersionAsync(cancellationToken));

        Assert.Equal(2, _handler.Requests.Count);
        Assert.Single(_logger.Warnings);

        _time.Advance(TimeSpan.FromMinutes(1));
        await _checker.GetLatestVersionAsync(cancellationToken);
        Assert.Equal(3, _handler.Requests.Count);
    }

    [Fact]
    public async Task ANetworkFailureBeforeAnyAnswerGivesNoVersion()
    {
        _handler.Failure = new HttpRequestException("No route to host");

        Assert.Null(await _checker.GetLatestVersionAsync(TestContext.Current.CancellationToken));
        Assert.Single(_logger.Warnings);
    }

    [Fact]
    public async Task TabsAskingAtTheSameTimeShareOneRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var answers = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => _checker.GetLatestVersionAsync(cancellationToken)));

        Assert.All(answers, answer => Assert.Equal("1.4.0", answer));
        Assert.Single(_handler.Requests);
    }

    public void Dispose() => _checker.Dispose();
}
