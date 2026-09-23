using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Web.Resources;

namespace SpeedtestWatcher.Web.Ui.Display;

public static class IntegrationText
{
    public static string Label(string integration, string field) =>
        OwnLabel(integration, field) ?? CommonLabel(field) ?? Humanize(field);

    public static string? Placeholder(string integration, string field, string? schemaPlaceholder) =>
        OwnPlaceholder(integration, field) ?? CommonPlaceholder(field) ?? schemaPlaceholder;

    private static string? CommonLabel(string field) => field switch
    {
        "url" => WebStrings.IntegrationFieldUrl,
        "token" => WebStrings.IntegrationFieldToken,
        "display_name" => WebStrings.IntegrationFieldDisplayName,
        "send_failed" => WebStrings.IntegrationFieldSendFailed,
        "send_unhealthy" => WebStrings.IntegrationFieldSendUnhealthy,
        "unhealthy_message" => WebStrings.IntegrationFieldUnhealthyMessage,
        "send_healthy_again" => WebStrings.IntegrationFieldSendHealthyAgain,
        "healthy_again_message" => WebStrings.IntegrationFieldHealthyAgainMessage,
        "send_skipped" => WebStrings.IntegrationFieldSendSkipped,
        "send_connection_lost" => WebStrings.IntegrationFieldSendConnectionLost,
        "connection_lost_message" => WebStrings.IntegrationFieldConnectionLostMessage,
        "send_connection_restored" => WebStrings.IntegrationFieldSendConnectionRestored,
        "connection_restored_message" => WebStrings.IntegrationFieldConnectionRestoredMessage,
        "skipped_message" => WebStrings.IntegrationFieldSkippedMessage,
        "send_finished" => WebStrings.IntegrationFieldSendFinished,
        "finished_message" => WebStrings.IntegrationFieldFinishedMessage,
        "error_message" => WebStrings.IntegrationFieldErrorMessage,
        "priority" => WebStrings.IntegrationFieldPriority,
        "tags" => WebStrings.IntegrationFieldTags,
        "title" => WebStrings.IntegrationFieldTitle,
        "interval" => WebStrings.IntegrationFieldInterval,
        _ => null
    };

    private static string? OwnLabel(string integration, string field) => (integration, field) switch
    {
        ("discord", "url") => WebStrings.IntegrationFieldWebhookUrl,
        ("telegram", "token") => WebStrings.IntegrationTelegramToken,
        ("telegram", "chat_id") => WebStrings.IntegrationTelegramChatId,
        ("gotify", "key") => WebStrings.IntegrationFieldAppToken,
        ("pushover", "token") => WebStrings.IntegrationFieldAppToken,
        ("pushover", "user_key") => WebStrings.IntegrationPushoverUserKey,
        ("apprise", "url") => WebStrings.IntegrationAppriseUrl,
        ("apprise", "urls") => WebStrings.IntegrationAppriseUrls,
        ("apprise", "key") => WebStrings.IntegrationAppriseKey,
        ("healthChecks", "url") => WebStrings.IntegrationHealthchecksUrl,
        ("healthChecks", "interval") => WebStrings.IntegrationHealthchecksInterval,
        ("ntfy", "topic") => WebStrings.IntegrationNtfyTopic,
        ("ntfy", "token") => WebStrings.IntegrationNtfyToken,
        ("ntfy", "priority") => WebStrings.IntegrationNtfyPriority,
        ("ntfy", "error_priority") => WebStrings.IntegrationNtfyErrorPriority,
        ("webhook", "url") => WebStrings.IntegrationFieldWebhookUrl,
        ("webhook", "send_started") => WebStrings.IntegrationWebhookSendStarted,
        ("webhook", "send_alive") => WebStrings.IntegrationWebhookSendAlive,
        ("webhook", "send_recommendations") => WebStrings.IntegrationWebhookSendRecommendations,
        ("webhook", "send_unhealthy") => WebStrings.IntegrationWebhookSendUnhealthy,
        ("webhook", "send_healthy_again") => WebStrings.IntegrationWebhookSendHealthyAgain,
        ("webhook", "send_skipped") => WebStrings.IntegrationWebhookSendSkipped,
        ("webhook", "send_connection_lost") => WebStrings.IntegrationWebhookSendConnectionLost,
        ("webhook", "send_connection_restored") => WebStrings.IntegrationWebhookSendConnectionRestored,
        ("webhook", "send_config_updates") => WebStrings.IntegrationWebhookSendConfigUpdates,
        ("webhook", "interval") => WebStrings.IntegrationWebhookInterval,
        _ => null
    };

    private static string? CommonPlaceholder(string field) => field switch
    {
        "finished_message" => "%year%-%month%-%day% %hour%:%minute% — %ping% ms, %download% Mbps, %upload% Mbps",
        "unhealthy_message" => WebStrings.IntegrationExampleUnhealthyMessage,
        "healthy_again_message" => WebStrings.IntegrationExampleHealthyAgainMessage,
        "skipped_message" => WebStrings.IntegrationExampleSkippedMessage,
        "connection_lost_message" => WebStrings.IntegrationExampleConnectionLostMessage,
        "connection_restored_message" => WebStrings.IntegrationExampleConnectionRestoredMessage,
        "error_message" => WebStrings.IntegrationExampleErrorMessage,
        _ => null
    };

    private static string? OwnPlaceholder(string integration, string field) => (integration, field) switch
    {
        ("discord", "url") => "https://discord.com/api/webhooks/...",
        ("discord", "display_name") => WebStrings.Format(WebStrings.IntegrationExampleDiscordName, ProjectInfo.Name),
        ("gotify", "key") => WebStrings.IntegrationExampleAppToken,
        ("pushover", "token") => WebStrings.IntegrationExampleApiToken,
        ("pushover", "user_key") => WebStrings.IntegrationExampleUserKey,
        ("telegram", "token") => WebStrings.IntegrationExampleBotToken,
        ("telegram", "chat_id") => WebStrings.IntegrationExampleChatId,
        ("apprise", "url") => "http://apprise:8000",
        ("apprise", "urls") => "discord://id/token, mailto://user:pass@example.com",
        ("apprise", "key") => "apprise",
        ("apprise", "tags") => WebStrings.IntegrationExampleAppriseTags,
        ("apprise", "title") => ProjectInfo.Name,
        ("healthChecks", "url") => "https://hc-ping.com/<uuid>",
        ("ntfy", "url") => "https://ntfy.sh",
        ("ntfy", "topic") => "speedtest-watcher-alerts",
        ("ntfy", "token") => WebStrings.IntegrationExampleNtfyToken,
        ("ntfy", "tags") => "warning,satellite",
        ("ntfy", "title") => ProjectInfo.Name,
        ("webhook", "url") => "https://your-server.com/hook",
        _ => null
    };

    private static string Humanize(string field)
    {
        var words = field.Replace('_', ' ');
        return words.Length == 0 ? field : char.ToUpperInvariant(words[0]) + words[1..];
    }
}
