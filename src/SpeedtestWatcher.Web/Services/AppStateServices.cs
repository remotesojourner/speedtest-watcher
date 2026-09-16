using System.Text.Json;
using Microsoft.JSInterop;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Web.Services;


public class PreferencesService
{
    private readonly IJSRuntime _js;
    private readonly ConfigStateService _config;
    public string TimeFormat { get; private set; } = "24h";
    public string SpeedUnit { get; private set; } = "mbps";
    public event Action? OnChange;

    /// <summary>The date layout is an instance setting, unlike the two above which are per browser.</summary>
    public string DateFormat => _config.CurrentConfig.DateFormat ?? "dmy";

    public PreferencesService(IJSRuntime js, ConfigStateService config)
    {
        _js = js;
        _config = config;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("speedtestWatcherInterop.getLocalStorage", "preferences");
            if (!string.IsNullOrEmpty(json))
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    if (dict.TryGetValue("timeFormat", out var tf)) TimeFormat = tf;
                    if (dict.TryGetValue("speedUnit", out var su)) SpeedUnit = su;
                }
            }
            OnChange?.Invoke();
        }
        catch { }
    }

    public async Task SetPreferencesAsync(string timeFormat, string speedUnit)
    {
        TimeFormat = timeFormat;
        SpeedUnit = speedUnit;
        try
        {
            var json = JsonSerializer.Serialize(new { timeFormat, speedUnit });
            await _js.InvokeVoidAsync("speedtestWatcherInterop.setLocalStorage", "preferences", json);
        }
        catch { }
        OnChange?.Invoke();
    }

    public double ConvertSpeed(double? mbps) => FormatHelper.ConvertSpeed(mbps, SpeedUnit);
    public string FormatTime(DateTime dt) => FormatHelper.FormatTime(dt, TimeFormat);
    public string FormatDate(DateTime dt) => FormatHelper.FormatDate(dt, DateFormat);
    public string FormatDateTime(DateTime dt) => FormatHelper.FormatDateTime(dt, TimeFormat, DateFormat);
}

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

public class SpeedtestStateService
{
    public List<SpeedtestDto> Tests { get; private set; } = [];
    public bool Loading { get; private set; }
    public bool HasMore { get; private set; } = true;
    public int? LastId => Tests.Count > 0 ? Tests.Last().Id : null;
    public SpeedtestDto? LatestTest => Tests.FirstOrDefault();
    public event Action? OnChange;

    public void SetTests(List<SpeedtestDto> tests, bool hasMore = true)
    {
        Tests = tests;
        HasMore = hasMore;
        OnChange?.Invoke();
    }

    public void PrependTest(SpeedtestDto test)
    {
        if (!Tests.Any(t => t.Id == test.Id))
        {
            Tests.Insert(0, test);
            OnChange?.Invoke();
        }
    }

    public void RemoveTest(int id)
    {
        Tests.RemoveAll(t => t.Id == id);
        OnChange?.Invoke();
    }

    public void SetLoading(bool loading)
    {
        Loading = loading;
        OnChange?.Invoke();
    }
}

public class ConfigStateService
{
    public ConfigDto CurrentConfig { get; private set; } = new();
    public bool Loaded { get; private set; }
    public event Action? OnChange;

    public void SetConfig(ConfigDto config)
    {
        CurrentConfig = config;
        Loaded = true;
        OnChange?.Invoke();
    }
}
