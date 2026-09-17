namespace SpeedtestWatcher.Core.Interfaces;

public interface IPauseStateService
{
    bool IsPaused { get; }
    bool IsRunning { get; }
    DateTime? ResumesAt { get; }
    void SetRunning(bool running);
    void Pause(double? hours);
    void Resume();
    event Action? OnStatusChanged;
}
