using SpeedtestWatcher.Application.Resources;

namespace SpeedtestWatcher.Application.Settings;

public sealed record SettingDefinition(string Key, string Default, SettingVisibility Visibility, Func<string, string?>? Validate = null)
{
    public bool IsManagedOnSecurityTab => Visibility is SettingVisibility.SecurityTab or SettingVisibility.Secret;

    public bool IsVisible(bool fullAccess) => Visibility switch
    {
        SettingVisibility.Everyone => true,
        SettingVisibility.FullAccessOnly or SettingVisibility.SecurityTab => fullAccess,
        _ => false
    };

    public string? ProblemWith(string? value) =>
        string.IsNullOrWhiteSpace(value) ? ApplicationStrings.SettingValueRequired : Validate?.Invoke(value);
}
