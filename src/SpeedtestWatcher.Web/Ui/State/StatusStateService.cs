using SpeedtestWatcher.Application.Monitoring;

namespace SpeedtestWatcher.Web.Ui.State;

public class StatusStateService
{
    public bool Running { get; private set; }
    public bool Paused { get; private set; }
    public ConnectionSnapshot Connection { get; private set; } = new(ConnectionHealth.Unknown, null, null, null);
    public event Action? OnChange;

    public void UpdateConnection(ConnectionSnapshot connection)
    {
        Connection = connection;
        OnChange?.Invoke();
    }

    public void UpdateStatus(bool running, bool paused)
    {
        Running = running;
        Paused = paused;
        OnChange?.Invoke();
    }
}
