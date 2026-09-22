using System.Collections.Frozen;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Cronos;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Application.Settings;

public static partial class SettingDefinitions
{
    public const string Unset = "none";

    public const int MaxRetentionDays = 10000;

    private const string NumberNeeded = "You need to provide a number in order to change this";
    private const string UrlNeeded = "You need to provide a valid URL in order to change this";

    public static IReadOnlyList<SettingDefinition> All { get; } =
    [
        new("ping", "25", SettingVisibility.Everyone, Number),
        new("download", "100", SettingVisibility.Everyone, Number),
        new("upload", "50", SettingVisibility.Everyone, Number),

        new("cron", "0 * * * *", SettingVisibility.FullAccessOnly, Cron),
        new("scheduleOffset", "true", SettingVisibility.FullAccessOnly, Boolean),

        new("provider", SpeedtestProvider.None.ToName(), SettingVisibility.Everyone, OneOf<SpeedtestProvider>("You need to provide a valid provider")),
        new("interface", Unset, SettingVisibility.Everyone),
        new("libreUrl", Unset, SettingVisibility.FullAccessOnly, UnsetOr(HttpUrl)),
        new("serverMode", ServerMode.Auto.ToName(), SettingVisibility.Everyone, OneOf<ServerMode>("You need to provide a valid server mode")),
        new("serverListMode", ServerListMode.Allow.ToName(), SettingVisibility.Everyone, OneOf<ServerListMode>("You need to provide a valid server list mode")),
        new("ooklaId", Unset, SettingVisibility.FullAccessOnly, UnsetOr(ServerId)),
        new("libreId", Unset, SettingVisibility.FullAccessOnly, UnsetOr(ServerId)),
        new("ooklaServerIds", Unset, SettingVisibility.FullAccessOnly, UnsetOr(ServerIds)),
        new("libreServerIds", Unset, SettingVisibility.FullAccessOnly, UnsetOr(ServerIds)),

        new("internetCheckEnabled", "true", SettingVisibility.Everyone, Boolean),
        new("internetCheckUrl", "https://icanhazip.com", SettingVisibility.FullAccessOnly, HttpUrl),
        new("skipIps", Unset, SettingVisibility.FullAccessOnly, UnsetOr(IpAddresses)),

        new("chartRange", "7d", SettingVisibility.Everyone, OneOf("You need to provide a valid chart range", "24h", "7d", "30d")),
        new("chartBeginAtZero", "false", SettingVisibility.Everyone, Boolean),
        new("dateFormat", "dmy", SettingVisibility.Everyone, OneOf("You need to provide a valid date format", "dmy", "mdy", "ymd")),

        new("retentionDays", "365", SettingVisibility.Everyone, RetentionDays),

        new("authEnabled", "false", SettingVisibility.SecurityTab, Boolean),
        new("visitorAccess", VisitorAccess.None.ToName(), SettingVisibility.SecurityTab, OneOf<VisitorAccess>("You need to provide a valid visitor access level")),
        new("oidcAuthority", Unset, SettingVisibility.SecurityTab, UnsetOr(HttpUrl)),
        new("oidcClientId", Unset, SettingVisibility.SecurityTab),
        new("oidcClientSecret", Unset, SettingVisibility.Secret),
        new("oidcScopes", "openid profile email", SettingVisibility.SecurityTab),
        new("apiTokenHash", Unset, SettingVisibility.Secret)
    ];

    public static IReadOnlyList<string> ObsoleteKeys { get; } = ["password", "passwordLevel"];

    private static readonly FrozenDictionary<string, SettingDefinition> ByKey = All.ToFrozenDictionary(definition => definition.Key);

    public static SettingDefinition? Find(string key) => ByKey.GetValueOrDefault(key);

    private static string? Number(string value) => NumberPattern().IsMatch(value) ? null : NumberNeeded;

    private static string? ServerId(string value) => DigitsPattern().IsMatch(value) ? null : NumberNeeded;

    private static string? ServerIds(string value) =>
        DigitListPattern().IsMatch(value) ? null : "Server IDs need to be numbers separated by commas";

    private static string? HttpUrl(string value) => WebAddress.IsHttp(value) ? null : UrlNeeded;

    private static string? Boolean(string value) =>
        value is "true" or "false" ? null : "You need to provide a boolean in order to change this";

    private static string? IpAddresses(string value) =>
        value.Split(',').All(part => IPAddress.TryParse(part.Trim(), out _)) ? null : "The skip list needs IP addresses separated by commas";

    private static string? RetentionDays(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) && days is >= 0 and <= MaxRetentionDays
            ? null
            : $"You need to provide a number between 0 and {MaxRetentionDays} in order to change this";

    private static string? Cron(string value)
    {
        try
        {
            CronExpression.Parse(value, CronFormat.Standard);
            return null;
        }
        catch (CronFormatException)
        {
            return "You need to provide a valid cron expression";
        }
    }

    private static Func<string, string?> UnsetOr(Func<string, string?> validate) =>
        value => value == Unset ? null : validate(value);

    private static Func<string, string?> OneOf(string problem, params string[] allowed) =>
        value => allowed.Contains(value) ? null : problem;

    private static Func<string, string?> OneOf<TEnum>(string problem) where TEnum : struct, Enum =>
        value => EnumNames.TryParse<TEnum>(value, out _) ? null : problem;

    [GeneratedRegex(@"^[0-9]+(\.[0-9]+)?$")]
    private static partial Regex NumberPattern();

    [GeneratedRegex("^[0-9]+$")]
    private static partial Regex DigitsPattern();

    [GeneratedRegex("^[0-9]+(,[0-9]+)*$")]
    private static partial Regex DigitListPattern();
}
