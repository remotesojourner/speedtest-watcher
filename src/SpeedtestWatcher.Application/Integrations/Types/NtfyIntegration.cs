namespace SpeedtestWatcher.Application.Integrations.Types;

internal sealed class NtfyIntegration : MessageIntegration
{
    public NtfyIntegration(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "ntfy";

    protected override string Title => "ntfy";

    protected override string Description => "Send push alerts via ntfy.sh or custom server";

    protected override IReadOnlyList<IntegrationFieldSchemaDto> OwnFields { get; } =
    [
        new() { Name = "url", Type = "text", Required = true, Regex = @"^https?://.+", Default = "https://ntfy.sh" },
        new() { Name = "topic", Type = "text", Required = true, Regex = @"^[A-Za-z0-9_\-]{1,64}$", Placeholder = "speedtest-watcher-alerts" },
        new() { Name = "token", Type = "text", Required = false },
        new() { Name = "title", Type = "text", Required = false, Default = "Speedtest Watcher Alert" },
        new() { Name = "tags", Type = "text", Required = false },
        new() { Name = "priority", Type = "text", Required = false, Regex = @"^[1-5]$", Default = "3" },
        new() { Name = "error_priority", Type = "text", Required = false, Regex = @"^[1-5]$", Default = "5" }
    ];

    protected override MessageTemplates Templates => MessageTemplates.PlainText;

    protected override string? SettingsProblem(IntegrationSettings settings) =>
        string.IsNullOrEmpty(settings.GetString("topic")) ? "The topic is missing" : null;

    protected override Task<IntegrationResult> SendMessageAsync(OutgoingMessage message, IntegrationSettings settings, CancellationToken cancellationToken)
    {
        var serverUrl = settings.GetString("url", "https://ntfy.sh").TrimEnd('/');
        var request = TextPost($"{serverUrl}/{settings.GetString("topic")}", message.Text);

        var priority = message.Kind is MessageKind.Failed or MessageKind.Unhealthy
            ? settings.GetString("error_priority", "5")
            : settings.GetString("priority", "3");
        request.Headers.Add("Priority", priority);

        AddHeaderWhenSet(request, "Title", settings.GetString("title"));
        AddHeaderWhenSet(request, "Tags", settings.GetString("tags"));
        if (settings.GetString("token") is { Length: > 0 } token) request.Headers.Add("Authorization", $"Bearer {token}");

        return SendAsync(request, cancellationToken);
    }

    private static void AddHeaderWhenSet(HttpRequestMessage request, string header, string value)
    {
        if (!string.IsNullOrEmpty(value)) request.Headers.Add(header, value);
    }
}
