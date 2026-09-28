using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Models.Dtos;

namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface IAppEventService
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
