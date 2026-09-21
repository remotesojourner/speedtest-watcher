namespace SpeedtestWatcher.Core.Settings;

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
        string.IsNullOrWhiteSpace(value) ? "You need to provide the new value" : Validate?.Invoke(value);
}
