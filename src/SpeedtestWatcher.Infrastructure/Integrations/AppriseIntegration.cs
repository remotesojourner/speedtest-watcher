using System.Net;
using System.Text.Json;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Integrations;

namespace SpeedtestWatcher.Infrastructure.Integrations;

public sealed class AppriseIntegration : MessageIntegration
{
    private static readonly string[] ProblemLevels = ["WARNING", "ERROR", "CRITICAL"];

    public AppriseIntegration(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "apprise";

    protected override string Title => "Apprise";

    protected override string Description => "Send alerts to any service Apprise supports, through an Apprise API server";

    protected override IReadOnlyList<IntegrationFieldSchemaDto> OwnFields { get; } =
    [
        new() { Name = "url", Type = "text", Required = true, Regex = @"^https?://.+" },
        new() { Name = "urls", Type = "textarea", Required = false },
        new() { Name = "key", Type = "text", Required = false, Regex = @"^[\w-]{1,128}$" },
        new() { Name = "tags", Type = "text", Required = false },
        new() { Name = "title", Type = "text", Required = false }
    ];

    protected override MessageTemplates Templates => MessageTemplates.PlainText;

    protected override string? SettingsProblem(IntegrationSettings settings)
    {
        if (string.IsNullOrEmpty(settings.GetString("url"))) return "The server URL is missing";

        return settings.GetString("urls").Length > 0 && settings.GetString("key").Length > 0
            ? "Use either Apprise URLs or a config key, not both"
            : null;
    }

    protected override Task<IntegrationResult> SendMessageAsync(OutgoingMessage message, IntegrationSettings settings, CancellationToken cancellationToken)
    {
        var serverUrl = settings.GetString("url").TrimEnd('/');
        var key = settings.GetString("key");
        var tags = settings.GetString("tags");

        var payload = new Dictionary<string, string>();
        AddWhenSet(payload, "urls", settings.GetString("urls"));
        AddWhenSet(payload, "tag", tags);
        AddWhenSet(payload, "title", settings.GetString("title"));
        payload["body"] = message.Text;
        payload["type"] = NotifyType(message.Kind);
        payload["format"] = "text";

        var url = key.Length == 0 ? $"{serverUrl}/notify/" : $"{serverUrl}/notify/{Uri.EscapeDataString(key)}";
        var request = JsonPost(url, payload);
        request.Headers.Add("Accept", "application/json");

        return SendAsync(request, cancellationToken, (status, reply) => status switch
        {
            HttpStatusCode.NoContent when key.Length == 0 => IntegrationResult.Failed("Apprise found no valid URLs to send to"),
            HttpStatusCode.NoContent => IntegrationResult.Failed($"Apprise has no configuration for the key {key}"),
            _ when IsSuccess(status) => null,
            _ => IntegrationResult.Failed(Explain(status, reply, tags))
        });
    }

    private string Explain(HttpStatusCode status, string reply, string tags)
    {
        if (AppriseReply.Parse(reply) is not { } parsed) return Answered(status, reply);

        if (status == HttpStatusCode.FailedDependency && parsed.Problems.Count == 0)
            return tags.Length > 0
                ? $"Apprise has nothing tagged {tags} to notify"
                : "Apprise has nothing untagged to notify. Add tags, or all, to choose what to notify";

        return Answered(status, parsed.Problems.Count == 0 ? parsed.Error : $"{parsed.Error}: {string.Join("; ", parsed.Problems)}");
    }

    private static string NotifyType(MessageKind kind) => kind switch
    {
        MessageKind.Finished => "success",
        MessageKind.Failed => "failure",
        MessageKind.Unhealthy => "warning",
        _ => "info"
    };

    private static void AddWhenSet(Dictionary<string, string> payload, string name, string value)
    {
        if (value.Length > 0) payload[name] = value;
    }

    private sealed record AppriseReply(string Error, IReadOnlyList<string> Problems)
    {
        public static AppriseReply? Parse(string reply)
        {
            try
            {
                using var document = JsonDocument.Parse(reply);
                if (document.RootElement is not { ValueKind: JsonValueKind.Object } root
                    || !root.TryGetProperty("error", out var error) || error.ValueKind != JsonValueKind.String)
                    return null;

                var problems = root.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array
                    ? details.EnumerateArray().Select(Problem).OfType<string>().ToList()
                    : [];

                return new AppriseReply(error.GetString()!, problems);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? Problem(JsonElement logEntry) =>
            logEntry.ValueKind == JsonValueKind.Array && logEntry.GetArrayLength() == 3
            && logEntry[0].ValueKind == JsonValueKind.String && ProblemLevels.Contains(logEntry[0].GetString())
            && logEntry[2].ValueKind == JsonValueKind.String
                ? logEntry[2].GetString()
                : null;
    }
}
