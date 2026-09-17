using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Integrations;

namespace SpeedtestWatcher.Infrastructure.Integrations;

public sealed class TelegramIntegration : MessageIntegration
{
    public TelegramIntegration(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "telegram";

    protected override string Title => "Telegram";

    protected override string Description => "Send test results via Telegram bot";

    protected override IReadOnlyList<IntegrationFieldSchemaDto> OwnFields { get; } =
    [
        new() { Name = "token", Type = "text", Required = true, Regex = @"(\d+):[a-zA-Z0-9_-]+", Placeholder = "Bot Token" },
        new() { Name = "chat_id", Type = "text", Required = true, Regex = @"\d+", Placeholder = "Chat ID" }
    ];

    protected override MessageTemplates Templates => MessageTemplates.TelegramMarkdown;

    protected override string? SettingsProblem(IntegrationSettings settings) =>
        string.IsNullOrEmpty(settings.GetString("token")) || string.IsNullOrEmpty(settings.GetString("chat_id"))
            ? "The bot token or chat ID is missing"
            : null;

    protected override Task<IntegrationResult> SendMessageAsync(OutgoingMessage message, IntegrationSettings settings, CancellationToken cancellationToken)
    {
        var payload = new { chat_id = settings.GetString("chat_id"), text = message.Text, parse_mode = "markdown" };
        return SendAsync(JsonPost($"https://api.telegram.org/bot{settings.GetString("token")}/sendMessage", payload), cancellationToken);
    }
}
