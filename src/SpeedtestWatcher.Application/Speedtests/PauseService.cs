using SpeedtestWatcher.Application.Security;
using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Application.Speedtests;

public sealed class PauseService
{
    private readonly RunState _state;
    private readonly ICurrentAccess _access;

    public PauseService(RunState state, ICurrentAccess access)
    {
        _state = state;
        _access = access;
    }

    public StatusDto GetStatus() => new() { Paused = _state.IsPaused, Running = _state.IsRunning };

    public OperationResult Pause(double? resumeInHours)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        if (resumeInHours > PauseRequest.MaxResumeInHours)
            return OperationResult.Invalid($"Speedtests can be paused for at most {PauseRequest.MaxResumeInHours:0} hours. Pause them indefinitely for a longer break.");

        _state.Pause(resumeInHours);
        return OperationResult.Ok();
    }

    public OperationResult Resume()
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        _state.Resume();
        return OperationResult.Ok();
    }
}
