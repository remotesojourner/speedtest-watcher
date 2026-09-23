using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Web.Ui.State;

public sealed partial class LiveUpdates : IDisposable
{
    private readonly IAppEvents _events;
    private readonly ConnectionState _connection;
    private readonly StatusStateService _status;
    private readonly RecentResults _results;
    private readonly SettingsState _settings;
    private readonly ICurrentAccess _access;
    private readonly ILogger<LiveUpdates> _logger;
    private Func<Func<Task>, Task>? _dispatch;

    public LiveUpdates(IAppEvents events, ConnectionState connection, StatusStateService status, RecentResults results, SettingsState settings, ICurrentAccess access, ILogger<LiveUpdates> logger)
    {
        _events = events;
        _connection = connection;
        _status = status;
        _results = results;
        _settings = settings;
        _access = access;
        _logger = logger;
    }

    public event Action<SpeedtestDto>? ResultArrived;

    public void Start(Func<Func<Task>, Task> dispatch)
    {
        if (_dispatch != null) return;

        _dispatch = dispatch;
        _status.UpdateConnection(_connection.Current);
        _events.ConnectionChanged += OnConnectionChanged;
        _events.TestStarted += OnTestStarted;
        _events.TestFinished += OnTestFinished;
        _events.RunStatusChanged += OnRunStatusChanged;
        _events.SettingsChanged += OnSettingsChanged;
    }

    public void Dispose()
    {
        _events.ConnectionChanged -= OnConnectionChanged;
        _events.TestStarted -= OnTestStarted;
        _events.TestFinished -= OnTestFinished;
        _events.RunStatusChanged -= OnRunStatusChanged;
        _events.SettingsChanged -= OnSettingsChanged;
        _dispatch = null;
    }

    private void OnConnectionChanged(ConnectionSnapshot connection) => Dispatch(() => _status.UpdateConnection(connection));

    private void OnTestStarted() => Dispatch(() => _status.UpdateStatus(true, _status.Paused));

    private void OnRunStatusChanged(RunStatus status) => Dispatch(() => _status.UpdateStatus(status.Running, status.Paused));

    private void OnTestFinished(SpeedtestDto published) => Dispatch(() =>
    {
        var result = _access.HasFullAccess ? published : published.WithoutPublicIp();
        _results.Add(result);
        _status.UpdateStatus(false, _status.Paused);
        ResultArrived?.Invoke(result);
    });

    private void OnSettingsChanged(IReadOnlyDictionary<string, string> changes) => Dispatch(_settings.LoadAsync);

    private void Dispatch(Action work) => Dispatch(() =>
    {
        work();
        return Task.CompletedTask;
    });

    private void Dispatch(Func<Task> work)
    {
        if (_dispatch is { } dispatch) _ = RunAsync(dispatch, work);
    }

    private async Task RunAsync(Func<Func<Task>, Task> dispatch, Func<Task> work)
    {
        try
        {
            await dispatch(work);
        }
        catch (Exception ex)
        {
            LogUpdateFailed(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "A live update couldn't be applied to this browser tab")]
    private partial void LogUpdateFailed(Exception exception);
}
