using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Integrations.Types;

internal sealed class DiscordIntegration : MessageIntegration
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
        new() { Name = "display_name", Type = "text", Required = false, Placeholder = ProjectInfo.Name }
    ];

    protected override MessageTemplates Templates => MessageTemplates.DiscordMarkdown;

    protected override string? SettingsProblem(IntegrationSettings settings) =>
        string.IsNullOrEmpty(settings.GetString("url")) ? "The webhook URL is missing" : null;

    protected override Task<IntegrationResult> SendMessageAsync(OutgoingMessage message, IntegrationSettings settings, CancellationToken cancellationToken)
    {
        var payload = new
        {
            username = settings.GetString("display_name", ProjectInfo.Name),
            embeds = new[]
            {
                new
                {
                    description = message.Text,
                    color = Colour(message.Kind),
                    footer = new { text = ProjectInfo.Name },
                    timestamp = DateTime.UtcNow.ToString("o")
                }
            }
        };

        return SendAsync(JsonPost(settings.GetString("url"), payload), cancellationToken);
    }

    private static int Colour(MessageKind kind) => kind switch
    {
        MessageKind.Finished or MessageKind.HealthyAgain => 4572762,
        MessageKind.Failed => 12993861,
        MessageKind.Unhealthy => 16098851,
        _ => 9807270
    };
}
