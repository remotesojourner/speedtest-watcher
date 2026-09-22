using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Web.Services;

public sealed class LiveUpdates : IDisposable
{
    private readonly IAppEvents _events;
    private readonly StatusStateService _status;
    private readonly RecentResults _results;
    private readonly SettingsState _settings;
    private readonly ILogger<LiveUpdates> _logger;
    private Func<Func<Task>, Task>? _dispatch;

    public LiveUpdates(IAppEvents events, StatusStateService status, RecentResults results, SettingsState settings, ILogger<LiveUpdates> logger)
    {
        _events = events;
        _status = status;
        _results = results;
        _settings = settings;
        _logger = logger;
    }

    public event Action<SpeedtestDto>? ResultArrived;

    public void Start(Func<Func<Task>, Task> dispatch)
    {
        if (_dispatch != null) return;

        _dispatch = dispatch;
        _events.TestStarted += OnTestStarted;
        _events.TestFinished += OnTestFinished;
        _events.RunStatusChanged += OnRunStatusChanged;
        _events.SettingsChanged += OnSettingsChanged;
    }

    public void Dispose()
    {
        _events.TestStarted -= OnTestStarted;
        _events.TestFinished -= OnTestFinished;
        _events.RunStatusChanged -= OnRunStatusChanged;
        _events.SettingsChanged -= OnSettingsChanged;
        _dispatch = null;
    }

    private void OnTestStarted() => Dispatch(() => _status.UpdateStatus(true, _status.Paused));

    private void OnRunStatusChanged(RunStatus status) => Dispatch(() => _status.UpdateStatus(status.Running, status.Paused));

    private void OnTestFinished(SpeedtestDto result) => Dispatch(() =>
    {
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
            _logger.LogWarning(ex, "A live update couldn't be applied to this browser tab");
        }
    }
}
