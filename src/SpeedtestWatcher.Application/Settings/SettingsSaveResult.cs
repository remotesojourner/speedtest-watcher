namespace SpeedtestWatcher.Application.Settings;

public sealed record SettingsSaveResult(string? Error)
{
    public static SettingsSaveResult Saved { get; } = new((string?)null);

    public bool Succeeded => Error == null;
}
