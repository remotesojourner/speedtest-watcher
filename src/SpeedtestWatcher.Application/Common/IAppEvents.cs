using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Common;

public interface IAppEvents
{
    event Action? TestStarted;

    event Action<SpeedtestDto>? TestFinished;

    event Action<RunStatus>? RunStatusChanged;

    event Action<IReadOnlyDictionary<string, string>>? SettingsChanged;

    event Action<ConnectionSnapshot>? ConnectionChanged;

    void PublishTestStarted();

    void PublishTestFinished(SpeedtestDto result);

    void PublishRunStatusChanged(RunStatus status);

    void PublishSettingsChanged(IReadOnlyDictionary<string, string> changes);

    void PublishConnectionChanged(ConnectionSnapshot connection);
}
