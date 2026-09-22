namespace SpeedtestWatcher.Application.Integrations;

internal sealed record MessageTemplates(string Finished, string Failed, string Unhealthy, string HealthyAgain, string Skipped)
{
    public static MessageTemplates DiscordMarkdown { get; } = new(
        ":sparkles: **A speedtest is finished**\n > :ping_pong: `Ping`: %ping% ms (±%jitter% ms)\n > :arrow_up: `Upload`: %upload% Mbps\n > :arrow_down: `Download`: %download% Mbps",
        ":x: **A speedtest has failed**\n > `Reason`: %error%",
        ":warning: **A speedtest missed your targets for %missed%**\n > :ping_pong: `Ping`: %ping% ms (target %threshold_ping% ms)\n > :arrow_down: `Download`: %download% Mbps (target %threshold_download% Mbps)\n > :arrow_up: `Upload`: %upload% Mbps (target %threshold_upload% Mbps)",
        ":white_check_mark: **Speedtests are meeting your targets again**\n > :ping_pong: `Ping`: %ping% ms (target %threshold_ping% ms)\n > :arrow_down: `Download`: %download% Mbps (target %threshold_download% Mbps)\n > :arrow_up: `Upload`: %upload% Mbps (target %threshold_upload% Mbps)",
        ":fast_forward: **A speedtest was skipped**\n > `Reason`: %error%");

    public static MessageTemplates TelegramMarkdown { get; } = new(
        "✨ *A speedtest is finished*\n🏓 `Ping`: %ping% ms (±%jitter% ms)\n🔼 `Upload`: %upload% Mbps\n🔽 `Download`: %download% Mbps",
        "❌ *A speedtest has failed*\n`Reason`: %error%",
        "⚠️ *A speedtest missed your targets for %missed%*\n🏓 `Ping`: %ping% ms (target %threshold_ping% ms)\n🔽 `Download`: %download% Mbps (target %threshold_download% Mbps)\n🔼 `Upload`: %upload% Mbps (target %threshold_upload% Mbps)",
        "✅ *Speedtests are meeting your targets again*\n🏓 `Ping`: %ping% ms (target %threshold_ping% ms)\n🔽 `Download`: %download% Mbps (target %threshold_download% Mbps)\n🔼 `Upload`: %upload% Mbps (target %threshold_upload% Mbps)",
        "⏭️ *A speedtest was skipped*\n`Reason`: %error%");

    public static MessageTemplates PlainText { get; } = new(
        "A speedtest is finished:\nPing: %ping% ms (±%jitter% ms)\nUpload: %upload% Mbps\nDownload: %download% Mbps",
        "A speedtest has failed. Reason: %error%",
        "A speedtest missed your targets for %missed%:\nPing: %ping% ms (target %threshold_ping% ms)\nDownload: %download% Mbps (target %threshold_download% Mbps)\nUpload: %upload% Mbps (target %threshold_upload% Mbps)",
        "Speedtests are meeting your targets again:\nPing: %ping% ms (target %threshold_ping% ms)\nDownload: %download% Mbps (target %threshold_download% Mbps)\nUpload: %upload% Mbps (target %threshold_upload% Mbps)",
        "A speedtest was skipped. Reason: %error%");

    public string For(MessageKind kind) => kind switch
    {
        MessageKind.Finished => Finished,
        MessageKind.Failed => Failed,
        MessageKind.Unhealthy => Unhealthy,
        MessageKind.HealthyAgain => HealthyAgain,
        _ => Skipped
    };
}
