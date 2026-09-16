using System.Text.Json;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Web.Services;

public class PreferencesService
{
    private readonly BrowserInterop _browser;
    private readonly ConfigStateService _config;
    private readonly ILogger<PreferencesService> _logger;
    public string TimeFormat { get; private set; } = "24h";
    public string SpeedUnit { get; private set; } = "mbps";
    public event Action? OnChange;

    public string DateFormat => _config.CurrentConfig.DateFormat ?? "dmy";

    public PreferencesService(BrowserInterop browser, ConfigStateService config, ILogger<PreferencesService> logger)
    {
        _browser = browser;
        _config = config;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var saved = ParseSaved(await _browser.GetLocalStorageAsync("preferences"));
        if (saved.TryGetValue("timeFormat", out var timeFormat)) TimeFormat = timeFormat;
        if (saved.TryGetValue("speedUnit", out var speedUnit)) SpeedUnit = speedUnit;
        OnChange?.Invoke();
    }

    public async Task SetPreferencesAsync(string timeFormat, string speedUnit)
    {
        TimeFormat = timeFormat;
        SpeedUnit = speedUnit;
        await _browser.SetLocalStorageAsync("preferences", JsonSerializer.Serialize(new { timeFormat, speedUnit }));
        OnChange?.Invoke();
    }

    public double ConvertSpeed(double? mbps) => FormatHelper.ConvertSpeed(mbps, SpeedUnit);
    public string FormatTime(DateTime dt) => FormatHelper.FormatTime(dt, TimeFormat);
    public string FormatDate(DateTime dt) => FormatHelper.FormatDate(dt, DateFormat);
    public string FormatDateTime(DateTime dt) => FormatHelper.FormatDateTime(dt, TimeFormat, DateFormat);

    private Dictionary<string, string> ParseSaved(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "The display preferences saved in this browser can't be read, so the defaults are used");
            return new();
        }
    }
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
    public bool LoadFailed { get; private set; }
    public bool HasMore { get; private set; } = true;
    public int? LastId => Tests.Count > 0 ? Tests.Last().Id : null;
    public SpeedtestDto? LatestTest => Tests.FirstOrDefault();
    public event Action? OnChange;

    public void SetTests(List<SpeedtestDto> tests, bool hasMore = true)
    {
        Tests = tests;
        HasMore = hasMore;
        LoadFailed = false;
        OnChange?.Invoke();
    }

    public void SetLoadFailed()
    {
        LoadFailed = true;
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
    private readonly ApiClient _api;

    public ConfigStateService(ApiClient api)
    {
        _api = api;
    }

    public ConfigDto CurrentConfig { get; private set; } = new();
    public bool Loaded { get; private set; }
    public event Action? OnChange;

    public void SetConfig(ConfigDto config)
    {
        CurrentConfig = config;
        Loaded = true;
        OnChange?.Invoke();
    }

    public async Task<ApiResult> SaveAsync(params (string Key, string Value)[] changes)
    {
        foreach (var (key, value) in changes)
        {
            var result = await _api.PatchAsync($"/api/config/{key}", new UpdateConfigKeyRequest { Value = value });
            if (!result.Succeeded) return result;

            CurrentConfig[key] = value;
        }

        return ApiResult.Ok;
    }

    public async Task<bool> ReloadAsync()
    {
        var result = await _api.GetAsync<ConfigDto>("/api/config");
        if (result.Value == null) return false;

        SetConfig(result.Value);
        return true;
    }
}
