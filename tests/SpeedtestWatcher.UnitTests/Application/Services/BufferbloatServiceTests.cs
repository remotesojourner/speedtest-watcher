using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using SpeedtestWatcher.TestSupport;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Repositories.Interfaces;
using SpeedtestWatcher.Application.Services;

namespace SpeedtestWatcher.UnitTests.Application.Services;

public class BufferbloatServiceTests
{
    private static readonly BufferbloatSampling _quickly = new(
        IdleWindow: TimeSpan.FromMilliseconds(250),
        Cadence: TimeSpan.FromMilliseconds(5),
        Timeout: TimeSpan.FromMilliseconds(100),
        LeastIdleSamples: 5,
        LeastLoadSamples: 10,
        LeastLoadTime: TimeSpan.FromMilliseconds(100));

    private readonly OfflineProbe _probe = new(10);
    private readonly ScriptedTraffic _traffic = new();

    [Fact]
    public async Task ALineThatSlowsUnderLoadIsMeasuredAgainstItsIdleSelf()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var measuring = await Meter(_quickly).StartAsync(cancellationToken);
        _traffic.Moving = LoadDirection.Download;
        _probe.Milliseconds = 60;
        await Task.Delay(250, cancellationToken);

        var reading = await measuring.StopAsync();

        Assert.NotNull(reading);
        Assert.Equal((50, 10, 60), (reading.DownloadMilliseconds, reading.IdleMilliseconds, reading.LoadedMilliseconds));
    }

    [Fact]
    public async Task EachDirectionIsMeasuredWhileTheBytesAreGoingThatWay()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var measuring = await Meter(_quickly).StartAsync(cancellationToken);

        _traffic.Moving = LoadDirection.Download;
        _probe.Milliseconds = 30;
        await Task.Delay(250, cancellationToken);
        _traffic.Moving = LoadDirection.Upload;
        _probe.Milliseconds = 90;
        await Task.Delay(250, cancellationToken);

        var reading = await measuring.StopAsync();

        Assert.Equal((20, 80), (reading!.DownloadMilliseconds, reading.UploadMilliseconds));
        Assert.Equal(10, reading.IdleMilliseconds);
    }

    [Fact]
    public async Task TooFewSamplesLeaveTheTestWithoutAFigure()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var demanding = _quickly with { LeastIdleSamples = 10_000 };

        await using var measuring = await Meter(demanding).StartAsync(cancellationToken);
        await Task.Delay(100, cancellationToken);

        Assert.Null(await measuring.StopAsync());
    }

    [Fact]
    public async Task RoundsThatGoUnansweredAreNotCountedAsFastAnswers()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var measuring = await Meter(_quickly).StartAsync(cancellationToken);
        _probe.Milliseconds = null;
        await Task.Delay(250, cancellationToken);

        Assert.Null(await measuring.StopAsync());
    }

    [Fact]
    public async Task WithoutProbeTargetsNothingIsSampled()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var measuring = await Meter(_quickly, targets: "").StartAsync(cancellationToken);
        await Task.Delay(100, cancellationToken);

        Assert.Null(await measuring.StopAsync());
    }

    private BufferbloatService Meter(BufferbloatSampling sampling, string targets = "1.1.1.1:443,8.8.8.8:443")
    {
        var settingsRepository = A.Fake<ISettingsRepository>();
        var settings = AppSettings.From(new Dictionary<string, string> { ["monitoringTargets"] = targets });
        A.CallTo(() => settingsRepository.GetAsync(A<CancellationToken>._)).Returns(settings);
        return new BufferbloatService(_probe, _traffic, settingsRepository, TimeProvider.System, sampling, NullLogger<BufferbloatService>.Instance);
    }
}
