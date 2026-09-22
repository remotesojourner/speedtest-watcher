namespace SpeedtestWatcher.Web.Ui.State;

public class StatusStateService
{
    public bool Running { get; private set; }
    public bool Paused { get; private set; }
    public event Action? OnChange;

    public void UpdateStatus(bool running, bool paused)
    {
        Running = running;
        Paused = paused;
        OnChange?.Invoke();
    }
}
