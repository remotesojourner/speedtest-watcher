using SpeedtestWatcher.Application.Resources;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Models.Dtos;

namespace SpeedtestWatcher.Application.Services.Integrations;

internal sealed class PushoverIntegrationService : MessageIntegrationService
{
    public PushoverIntegrationService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
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
            ? ApplicationStrings.PushoverSettingsMissing
            : null;

    protected override Task<IntegrationResult> SendMessageAsync(OutgoingMessage message, IntegrationSettings settings, CancellationToken cancellationToken)
    {
        var payload = new { token = settings.GetString("token"), user = settings.GetString("user_key"), message = message.Text };
        return SendAsync(JsonPost("https://api.pushover.net/1/messages.json", payload), cancellationToken);
    }
}
