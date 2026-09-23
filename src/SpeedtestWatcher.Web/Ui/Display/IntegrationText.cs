using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Web.Ui.Display;

public static class IntegrationText
{
    private static readonly Dictionary<string, string> _commonLabels = new()
    {
        ["url"] = "Server URL",
        ["token"] = "Token",
        ["display_name"] = "Display name",
        ["send_failed"] = "Send error messages",
        ["send_unhealthy"] = "Send alerts when a test misses your targets",
        ["unhealthy_message"] = "Target missed message",
        ["send_healthy_again"] = "Send a message when tests meet your targets again",
        ["healthy_again_message"] = "Targets met again message",
        ["send_skipped"] = "Send alerts when a test is skipped",
        ["send_connection_lost"] = "Send an alert when the connection goes down",
        ["connection_lost_message"] = "Connection lost message",
        ["send_connection_restored"] = "Send a message when the connection comes back",
        ["connection_restored_message"] = "Connection restored message",
        ["skipped_message"] = "Skipped message",
        ["send_finished"] = "Send finished messages",
        ["finished_message"] = "Finished message",
        ["error_message"] = "Error message",
        ["priority"] = "Priority",
        ["tags"] = "Tags",
        ["title"] = "Notification title",
        ["interval"] = "Interval (minutes)"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> _labels = new()
    {
        ["discord"] = new() { ["url"] = "Webhook URL" },
        ["telegram"] = new() { ["token"] = "Bot token", ["chat_id"] = "Chat ID" },
        ["gotify"] = new() { ["key"] = "App token" },
        ["pushover"] = new() { ["token"] = "App token", ["user_key"] = "User key" },
        ["apprise"] = new() { ["url"] = "Apprise API URL", ["urls"] = "Apprise URLs", ["key"] = "Config key (instead of URLs)" },
        ["healthChecks"] = new() { ["url"] = "Healthchecks URL", ["interval"] = "Ping interval (minutes)" },
        ["ntfy"] = new()
        {
            ["topic"] = "Topic",
            ["token"] = "Access token",
            ["priority"] = "Default priority (1-5)",
            ["error_priority"] = "Error priority (1-5)"
        },
        ["webhook"] = new()
        {
            ["url"] = "Webhook URL",
            ["send_started"] = "Send started messages",
            ["send_alive"] = "Send keep-alive messages",
            ["send_recommendations"] = "Send recommendations",
            ["send_unhealthy"] = "Send target-missed alerts",
            ["send_healthy_again"] = "Send targets-met-again messages",
            ["send_skipped"] = "Send skipped-test messages",
            ["send_connection_lost"] = "Send connection-lost messages",
            ["send_connection_restored"] = "Send connection-restored messages",
            ["send_config_updates"] = "Send configuration updates",
            ["interval"] = "Keep-alive interval (minutes)"
        }
    };

    private static readonly Dictionary<string, string> _commonPlaceholders = new()
    {
        ["finished_message"] = "%year%-%month%-%day% %hour%:%minute% — %ping% ms, %download% Mbps, %upload% Mbps",
        ["unhealthy_message"] = "%download% Mbps down, %upload% Mbps up, %ping% ms — targets %threshold_download%/%threshold_upload%/%threshold_ping%",
        ["healthy_again_message"] = "Back within your targets: %download% Mbps down, %upload% Mbps up, %ping% ms",
        ["skipped_message"] = "[%year%-%month%-%day% %hour%:%minute%] Skipped: %error%",
        ["connection_lost_message"] = "The line has been down since %since%",
        ["connection_restored_message"] = "The line is back after %downtime%",
        ["error_message"] = "[%year%-%month%-%day% %hour%:%minute%] Error: %error%"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> _placeholders = new()
    {
        ["discord"] = new() { ["url"] = "https://discord.com/api/webhooks/...", ["display_name"] = "Speedtest Watcher Notification" },
        ["apprise"] = new()
        {
            ["url"] = "http://apprise:8000",
            ["urls"] = "discord://id/token, mailto://user:pass@example.com",
            ["key"] = "apprise",
            ["tags"] = "Optional, e.g. admin, devops or all",
            ["title"] = ProjectInfo.Name
        },
        ["healthChecks"] = new() { ["url"] = "https://hc-ping.com/<uuid>" },
        ["ntfy"] = new()
        {
            ["url"] = "https://ntfy.sh",
            ["topic"] = "speedtest-watcher-alerts",
            ["token"] = "Optional, for protected servers",
            ["tags"] = "warning,satellite",
            ["title"] = ProjectInfo.Name
        },
        ["webhook"] = new() { ["url"] = "https://your-server.com/hook" }
    };

    public static string Label(string integration, string field)
    {
        if (_labels.TryGetValue(integration, out var own) && own.TryGetValue(field, out var label)) return label;
        if (_commonLabels.TryGetValue(field, out var common)) return common;
        return Humanize(field);
    }

    public static string? Placeholder(string integration, string field, string? schemaPlaceholder)
    {
        if (_placeholders.TryGetValue(integration, out var own) && own.TryGetValue(field, out var placeholder)) return placeholder;
        if (_commonPlaceholders.TryGetValue(field, out var common)) return common;
        return schemaPlaceholder;
    }

    private static string Humanize(string field)
    {
        var words = field.Replace('_', ' ');
        return words.Length == 0 ? field : char.ToUpperInvariant(words[0]) + words[1..];
    }
}
