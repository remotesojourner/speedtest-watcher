using SpeedtestWatcher.Application.Resources;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Models.Dtos;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.Services;

public sealed class PauseService
{
    public const double MaxResumeInHours = 720;

    private readonly RunStateService _state;
    private readonly ICurrentAccessService _access;

    public PauseService(RunStateService state, ICurrentAccessService access)
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
