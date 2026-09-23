using SpeedtestWatcher.Application.Resources;
using System.Collections.Frozen;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Cronos;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Monitoring;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Application.Settings;

public static partial class SettingDefinitions
{
    public const string Unset = "none";

    public const int MaxRetentionDays = 10000;


    public static IReadOnlyList<SettingDefinition> All { get; } =
    [
        new("ping", "25", SettingVisibility.Everyone, Number),
        new("download", "100", SettingVisibility.Everyone, Number),
        new("upload", "50", SettingVisibility.Everyone, Number),
        new("maxPacketLoss", Unset, SettingVisibility.Everyone, UnsetOr(Number)),
        new("maxBufferbloat", Unset, SettingVisibility.Everyone, UnsetOr(Number)),

        new("cron", "0 * * * *", SettingVisibility.FullAccessOnly, Cron),
        new("scheduleOffset", "true", SettingVisibility.FullAccessOnly, Boolean),
        new("unhealthyCron", Unset, SettingVisibility.FullAccessOnly, UnsetOr(Cron)),

        new("provider", SpeedtestProvider.None.ToName(), SettingVisibility.Everyone, OneOf<SpeedtestProvider>(() => ApplicationStrings.SettingInvalidProvider)),
        new("interface", Unset, SettingVisibility.Everyone),
        new("libreUrl", Unset, SettingVisibility.FullAccessOnly, UnsetOr(HttpUrl)),
        new("serverMode", ServerMode.Auto.ToName(), SettingVisibility.Everyone, OneOf<ServerMode>(() => ApplicationStrings.SettingInvalidServerMode)),
        new("serverListMode", ServerListMode.Allow.ToName(), SettingVisibility.Everyone, OneOf<ServerListMode>(() => ApplicationStrings.SettingInvalidServerListMode)),
        new("ooklaId", Unset, SettingVisibility.FullAccessOnly, UnsetOr(ServerId)),
        new("libreId", Unset, SettingVisibility.FullAccessOnly, UnsetOr(ServerId)),
        new("ooklaServerIds", Unset, SettingVisibility.FullAccessOnly, UnsetOr(ServerIds)),
        new("libreServerIds", Unset, SettingVisibility.FullAccessOnly, UnsetOr(ServerIds)),

        new("monitoringEnabled", "true", SettingVisibility.Everyone, Boolean),
        new("monitoringTargets", MonitoringSettings.DefaultTargets, SettingVisibility.FullAccessOnly, ProbeTargets),
        new("monitoringInterval", "15", SettingVisibility.FullAccessOnly, Seconds),
        new("monitoringRoundsDown", "3", SettingVisibility.FullAccessOnly, Rounds),
        new("monitoringRoundsUp", "2", SettingVisibility.FullAccessOnly, Rounds),
        new("monitoringTestAfterReconnect", "false", SettingVisibility.Everyone, Boolean),

        new("internetCheckEnabled", "true", SettingVisibility.Everyone, Boolean),
        new("internetCheckUrl", "https://icanhazip.com", SettingVisibility.FullAccessOnly, HttpUrl),
        new("skipIps", Unset, SettingVisibility.FullAccessOnly, UnsetOr(IpAddresses)),

        new("chartRange", "7d", SettingVisibility.Everyone, OneOf(() => ApplicationStrings.SettingInvalidChartRange, "24h", "7d", "30d")),
        new("chartBeginAtZero", "false", SettingVisibility.Everyone, Boolean),
        new("dateFormat", "dmy", SettingVisibility.Everyone, OneOf(() => ApplicationStrings.SettingInvalidDateFormat, "dmy", "mdy", "ymd")),

        new("retentionDays", "365", SettingVisibility.Everyone, RetentionDays),

        new("authEnabled", "false", SettingVisibility.SecurityTab, Boolean),
        new("visitorAccess", VisitorAccess.None.ToName(), SettingVisibility.SecurityTab, OneOf<VisitorAccess>(() => ApplicationStrings.SettingInvalidVisitorAccess)),
        new("oidcAuthority", Unset, SettingVisibility.SecurityTab, UnsetOr(HttpUrl)),
        new("oidcClientId", Unset, SettingVisibility.SecurityTab),
        new("oidcClientSecret", Unset, SettingVisibility.Secret),
        new("oidcScopes", "openid profile email", SettingVisibility.SecurityTab),
        new("apiTokenHash", Unset, SettingVisibility.Secret)
    ];

    public static IReadOnlyList<string> ObsoleteKeys { get; } = ["password", "passwordLevel"];

    private static readonly FrozenDictionary<string, SettingDefinition> _byKey = All.ToFrozenDictionary(definition => definition.Key);

    public static SettingDefinition? Find(string key) => _byKey.GetValueOrDefault(key);

    private static string? Number(string value) => NumberPattern().IsMatch(value) ? null : ApplicationStrings.SettingNumberNeeded;

    private static string? ServerId(string value) => DigitsPattern().IsMatch(value) ? null : ApplicationStrings.SettingNumberNeeded;

    private static string? ServerIds(string value) =>
        DigitListPattern().IsMatch(value) ? null : ApplicationStrings.SettingServerIdsInvalid;

    private static string? HttpUrl(string value) => WebAddress.IsHttp(value) ? null : ApplicationStrings.SettingUrlNeeded;

    private static string? Boolean(string value) =>
        value is "true" or "false" ? null : ApplicationStrings.SettingBooleanNeeded;

    private static string? ProbeTargets(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is { Length: > 0 } parts
        && parts.All(part => ProbeTarget.Parse(part) != null)
            ? null
            : ApplicationStrings.SettingProbeTargetsInvalid;

    private static string? Seconds(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
        && seconds is >= MonitoringSettings.ShortestInterval and <= MonitoringSettings.LongestInterval
            ? null
            : ApplicationStrings.Format(ApplicationStrings.SettingSecondsRange, MonitoringSettings.ShortestInterval, MonitoringSettings.LongestInterval);

    private static string? Rounds(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rounds) && rounds is >= 1 and <= MonitoringSettings.MostRounds
            ? null
            : ApplicationStrings.Format(ApplicationStrings.SettingRoundsRange, MonitoringSettings.MostRounds);

    private static string? IpAddresses(string value) =>
        value.Split(',').All(part => IPAddress.TryParse(part.Trim(), out _)) ? null : ApplicationStrings.SettingSkipIpsInvalid;

    private static string? RetentionDays(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) && days is >= 0 and <= MaxRetentionDays
            ? null
            : ApplicationStrings.Format(ApplicationStrings.SettingRetentionRange, MaxRetentionDays);

    private static string? Cron(string value)
    {
        try
        {
            CronExpression.Parse(value, CronFormat.Standard);
            return null;
        }
        catch (CronFormatException)
        {
            return ApplicationStrings.SettingCronInvalid;
        }
    }

    private static Func<string, string?> UnsetOr(Func<string, string?> validate) =>
        value => value == Unset ? null : validate(value);

    private static Func<string, string?> OneOf(Func<string> problem, params string[] allowed) =>
        value => allowed.Contains(value) ? null : problem();

    private static Func<string, string?> OneOf<TEnum>(Func<string> problem) where TEnum : struct, Enum =>
        value => EnumNames.TryParse<TEnum>(value, out _) ? null : problem();

    [GeneratedRegex(@"^[0-9]+(\.[0-9]+)?$")]
    private static partial Regex NumberPattern();

    [GeneratedRegex("^[0-9]+$")]
    private static partial Regex DigitsPattern();

    [GeneratedRegex("^[0-9]+(,[0-9]+)*$")]
    private static partial Regex DigitListPattern();
}
