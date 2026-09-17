using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Integrations;

namespace SpeedtestWatcher.Infrastructure.Integrations;

public sealed class GotifyIntegration : MessageIntegration
{
    private const int DefaultPriority = 5;
    private const int FailedPriority = 8;
    private const int MissedTargetsPriority = 7;

    public GotifyIntegration(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "gotify";

    protected override string Title => "Gotify";

    protected override string Description => "Send notifications to a Gotify server";

    protected override IReadOnlyList<IntegrationFieldSchemaDto> OwnFields { get; } =
    [
        new() { Name = "url", Type = "text", Required = true, Regex = @"https?://.+", Placeholder = "https://gotify.example.com" },
        new() { Name = "key", Type = "text", Required = true, Regex = @"^.{15}$", Placeholder = "App Token" },
        new() { Name = "priority", Type = "text", Required = true, Regex = @"^[0-9]$", Default = "5" }
    ];

    protected override MessageTemplates Templates => MessageTemplates.PlainText;

    protected override string? SettingsProblem(IntegrationSettings settings) =>
        string.IsNullOrEmpty(ServerUrl(settings)) || string.IsNullOrEmpty(settings.GetString("key"))
            ? "The server URL or app token is missing"
            : null;

    protected override Task<IntegrationResult> SendMessageAsync(OutgoingMessage message, IntegrationSettings settings, CancellationToken cancellationToken)
    {
        var priority = message.Kind switch
        {
            MessageKind.Failed => FailedPriority,
            MessageKind.Unhealthy => MissedTargetsPriority,
            _ => int.TryParse(settings.GetString("priority", DefaultPriority.ToString()), out var configured) ? configured : DefaultPriority
        };

        var request = JsonPost($"{ServerUrl(settings)}/message", new { message = message.Text, priority });
        request.Headers.Add("Authorization", $"Bearer {settings.GetString("key")}");
        return SendAsync(request, cancellationToken);
    }

    private static string ServerUrl(IntegrationSettings settings) => settings.GetString("url").TrimEnd('/');
}
