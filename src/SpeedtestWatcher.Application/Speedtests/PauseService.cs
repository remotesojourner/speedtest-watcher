using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Resources;
using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Application.Speedtests;

public sealed class PauseService
{
    public const double MaxResumeInHours = 720;

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

        if (resumeInHours > MaxResumeInHours)
            return OperationResult.Invalid(ApplicationStrings.Format(ApplicationStrings.PauseTooLong, MaxResumeInHours));

        _state.Pause(resumeInHours);
        return OperationResult.Ok();
    }

    public OperationResult SkipNextScheduledTest()
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        _state.SkipNextScheduledRun();
        return OperationResult.Ok();
    }

    public OperationResult Resume()
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        _state.Resume();
        return OperationResult.Ok();
    }
}
