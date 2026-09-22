using System.Globalization;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Core.Settings;

public sealed record AppSettings(
    TargetSettings Targets,
    ScheduleSettings Schedule,
    ProviderSettings Provider,
    PreTestCheckSettings PreTestChecks,
    DisplaySettings Display,
    int RetentionDays,
    SignInSettings SignIn)
{
    public static AppSettings Defaults { get; } = From(SettingDefinitions.All.ToDictionary(definition => definition.Key, definition => definition.Default));

    public static AppSettings From(IReadOnlyDictionary<string, string> values)
    {
        string? Optional(string key)
        {
            var value = values.GetValueOrDefault(key);
            return string.IsNullOrWhiteSpace(value) || value == SettingDefinitions.Unset ? null : value.Trim();
        }

        string Text(string key)
        {
            var definition = SettingDefinitions.Find(key)!;
            return Optional(key) is { } value && definition.ProblemWith(value) == null ? value : definition.Default;
        }

        bool Flag(string key) => Text(key) == "true";

        TEnum Choice<TEnum>(string key) where TEnum : struct, Enum =>
            EnumNames.TryParse<TEnum>(Optional(key), out var choice) ? choice : default;

        double? PositiveNumber(string key) =>
            double.TryParse(Optional(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && number > 0 ? number : null;

        IReadOnlyList<string> List(string key) =>
            Optional(key)?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

        return new AppSettings(
            Targets: new TargetSettings(
                PositiveNumber("ping") is { } ping ? (int)Math.Round(ping) : null,
                PositiveNumber("download"),
                PositiveNumber("upload")),
            Schedule: new ScheduleSettings(Text("cron"), Flag("scheduleOffset")),
            Provider: new ProviderSettings(
                Selected: Choice<SpeedtestProvider>("provider"),
                Interface: Optional("interface"),
                LibreUrl: Optional("libreUrl"),
                ServerMode: Choice<ServerMode>("serverMode"),
                ServerListMode: Choice<ServerListMode>("serverListMode"),
                Ookla: new ServerChoice(Optional("ooklaId"), List("ooklaServerIds")),
                Libre: new ServerChoice(Optional("libreId"), List("libreServerIds"))),
            PreTestChecks: new PreTestCheckSettings(Flag("internetCheckEnabled"), Text("internetCheckUrl"), List("skipIps")),
            Display: new DisplaySettings(Text("chartRange"), Flag("chartBeginAtZero"), Text("dateFormat")),
            RetentionDays: int.TryParse(Optional("retentionDays"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) ? days : 0,
            SignIn: new SignInSettings(
                Enabled: Flag("authEnabled"),
                VisitorAccess: Choice<VisitorAccess>("visitorAccess"),
                Authority: Optional("oidcAuthority"),
                ClientId: Optional("oidcClientId"),
                ClientSecret: Optional("oidcClientSecret"),
                Scopes: SignInSettings.ParseScopes(Optional("oidcScopes")),
                ApiTokenHash: Optional("apiTokenHash")));
    }
}
