using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Integrations;

namespace SpeedtestWatcher.Infrastructure.Integrations;

public sealed class DiscordIntegration : MessageIntegration
{
    public DiscordIntegration(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "discord";

    protected override string Title => "Discord";

    protected override string Description => "Send test results and alerts via Discord webhooks";

    protected override IReadOnlyList<IntegrationFieldSchemaDto> OwnFields { get; } =
    [
        new() { Name = "url", Type = "text", Required = true, Regex = @"https://.*discord\.com/api/webhooks/\d+/.+", Placeholder = "Webhook URL" },
        new() { Name = "display_name", Type = "text", Required = false, Placeholder = "Speedtest Watcher" }
    ];

    protected override MessageTemplates Templates => MessageTemplates.DiscordMarkdown;

    protected override string? MissingSetting(IntegrationSettings settings) =>
        string.IsNullOrEmpty(settings.GetString("url")) ? "The webhook URL is missing" : null;

    protected override Task<IntegrationResult> SendMessageAsync(OutgoingMessage message, IntegrationSettings settings, CancellationToken cancellationToken)
    {
        var payload = new
        {
            username = settings.GetString("display_name", "Speedtest Watcher"),
            embeds = new[]
            {
                new
                {
                    description = message.Text,
                    color = Colour(message.Kind),
                    footer = new { text = "Speedtest Watcher" },
                    timestamp = DateTime.UtcNow.ToString("o")
                }
            }
        };

        return SendAsync(JsonPost(settings.GetString("url"), payload), cancellationToken);
    }

    private static int Colour(MessageKind kind) => kind switch
    {
        MessageKind.Finished => 4572762,
        MessageKind.Failed => 12993861,
        MessageKind.Unhealthy => 16098851,
        _ => 9807270
    };
}
