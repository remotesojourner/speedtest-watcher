using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Integrations;

namespace SpeedtestWatcher.Infrastructure.Integrations;

public sealed class PushoverIntegration : MessageIntegration
{
    public PushoverIntegration(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "pushover";

    protected override string Title => "Pushover";

    protected override string Description => "Send push notifications via Pushover API";

    protected override IReadOnlyList<IntegrationFieldSchemaDto> OwnFields { get; } =
    [
        new() { Name = "token", Type = "text", Required = true, Regex = @"^[a-z0-9]{30}$", Placeholder = "API Token" },
        new() { Name = "user_key", Type = "text", Required = true, Regex = @"^[a-z0-9]{30}$", Placeholder = "User Key" }
    ];

    protected override MessageTemplates Templates => MessageTemplates.PlainText;

    protected override string? SettingsProblem(IntegrationSettings settings) =>
        string.IsNullOrEmpty(settings.GetString("token")) || string.IsNullOrEmpty(settings.GetString("user_key"))
            ? "The API token or user key is missing"
            : null;

    protected override Task<IntegrationResult> SendMessageAsync(OutgoingMessage message, IntegrationSettings settings, CancellationToken cancellationToken)
    {
        var payload = new { token = settings.GetString("token"), user = settings.GetString("user_key"), message = message.Text };
        return SendAsync(JsonPost("https://api.pushover.net/1/messages.json", payload), cancellationToken);
    }
}
