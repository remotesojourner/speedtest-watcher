namespace SpeedtestWatcher.Web.Helpers;

public static class IntegrationText
{
    private static readonly Dictionary<string, string> CommonLabels = new()
    {
        ["url"] = "Server URL",
        ["token"] = "Token",
        ["display_name"] = "Display name",
        ["send_failed"] = "Send error messages",
        ["send_unhealthy"] = "Send alerts when a test misses your targets",
        ["unhealthy_message"] = "Target missed message",
        ["send_skipped"] = "Send alerts when a test is skipped",
        ["skipped_message"] = "Skipped message",
        ["send_finished"] = "Send finished messages",
        ["finished_message"] = "Finished message",
        ["error_message"] = "Error message",
        ["priority"] = "Priority",
        ["tags"] = "Tags",
        ["title"] = "Notification title",
        ["interval"] = "Interval (minutes)"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Labels = new()
    {
        ["discord"] = new() { ["url"] = "Webhook URL" },
        ["telegram"] = new() { ["token"] = "Bot token", ["chat_id"] = "Chat ID" },
        ["gotify"] = new() { ["key"] = "App token" },
        ["pushover"] = new() { ["token"] = "App token", ["user_key"] = "User key" },
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
            ["send_skipped"] = "Send skipped-test messages",
            ["send_config_updates"] = "Send configuration updates",
            ["interval"] = "Keep-alive interval (minutes)"
        }
    };

    private static readonly Dictionary<string, string> CommonPlaceholders = new()
    {
        ["finished_message"] = "%year%-%month%-%day% %hour%:%minute% — %ping% ms, %download% Mbps, %upload% Mbps",
        ["unhealthy_message"] = "%download% Mbps down, %upload% Mbps up, %ping% ms — targets %threshold_download%/%threshold_upload%/%threshold_ping%",
        ["skipped_message"] = "[%year%-%month%-%day% %hour%:%minute%] Skipped: %error%",
        ["error_message"] = "[%year%-%month%-%day% %hour%:%minute%] Error: %error%"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Placeholders = new()
    {
        ["discord"] = new() { ["url"] = "https://discord.com/api/webhooks/...", ["display_name"] = "Speedtest Watcher Notification" },
        ["healthChecks"] = new() { ["url"] = "https://hc-ping.com/<uuid>" },
        ["ntfy"] = new()
        {
            ["url"] = "https://ntfy.sh",
            ["topic"] = "speedtest-watcher-alerts",
            ["token"] = "Optional, for protected servers",
            ["tags"] = "warning,satellite",
            ["title"] = "Speedtest Watcher"
        },
        ["webhook"] = new() { ["url"] = "https://your-server.com/hook" }
    };

    public static string Label(string integration, string field)
    {
        if (Labels.TryGetValue(integration, out var own) && own.TryGetValue(field, out var label)) return label;
        if (CommonLabels.TryGetValue(field, out var common)) return common;
        return Humanize(field);
    }

    public static string? Placeholder(string integration, string field, string? schemaPlaceholder)
    {
        if (Placeholders.TryGetValue(integration, out var own) && own.TryGetValue(field, out var placeholder)) return placeholder;
        if (CommonPlaceholders.TryGetValue(field, out var common)) return common;
        return schemaPlaceholder;
    }

    private static string Humanize(string field)
    {
        var words = field.Replace('_', ' ');
        return words.Length == 0 ? field : char.ToUpperInvariant(words[0]) + words[1..];
    }
}
