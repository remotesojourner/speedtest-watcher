using System.Text.Json;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Web.Ui.Display;

namespace SpeedtestWatcher.Web.Ui.State;

public class PreferencesService
{
    private readonly BrowserInterop _browser;
    private readonly SettingsState _settings;
    private readonly ILogger<PreferencesService> _logger;
    public string TimeFormat { get; private set; } = "24h";
    public string SpeedUnit { get; private set; } = "mbps";
    public TimeZoneInfo TimeZone { get; private set; } = TimeZoneInfo.Utc;
    public event Action? OnChange;

    public string DateFormat => _settings.Current.Display.DateFormat;

    public DateTime Now => ToLocal(DateTime.UtcNow);

    public PreferencesService(BrowserInterop browser, SettingsState settings, ILogger<PreferencesService> logger)
    {
        _browser = browser;
        _settings = settings;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var saved = ParseSaved(await _browser.GetLocalStorageAsync("preferences"));
        if (saved.TryGetValue("timeFormat", out var timeFormat)) TimeFormat = timeFormat;
        if (saved.TryGetValue("speedUnit", out var speedUnit)) SpeedUnit = speedUnit;
        if (await _browser.GetTimeZoneAsync() is { } browserTimeZone)
        {
            if (TimeZones.TryFindTimeZone(browserTimeZone, out var timeZone)) TimeZone = timeZone;
            else _logger.LogWarning("The browser's time zone {TimeZone} isn't known on this server, so times are shown in UTC", browserTimeZone);
        }
        OnChange?.Invoke();
    }

    public async Task SetPreferencesAsync(string timeFormat, string speedUnit)
    {
        TimeFormat = timeFormat;
        SpeedUnit = speedUnit;
        await _browser.SetLocalStorageAsync("preferences", JsonSerializer.Serialize(new { timeFormat, speedUnit }));
        OnChange?.Invoke();
    }

    public DateTime ToLocal(DateTime moment) => TimeZones.InTimeZone(moment, TimeZone);
    public DateTime ToLocal(string timestamp) => ToLocal(TimeZones.ParseUtcTimestamp(timestamp));
    public double ConvertSpeed(double? mbps) => DisplayFormat.ConvertSpeed(mbps, SpeedUnit);
    public string FormatTime(DateTime dt) => DisplayFormat.FormatTime(dt, TimeFormat);
    public string FormatDate(DateTime dt) => DisplayFormat.FormatDate(dt, DateFormat);
    public string FormatDateTime(DateTime dt) => DisplayFormat.FormatDateTime(dt, TimeFormat, DateFormat);

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
