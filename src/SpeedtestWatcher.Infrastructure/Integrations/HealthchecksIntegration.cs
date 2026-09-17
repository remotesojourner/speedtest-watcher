using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Events;
using SpeedtestWatcher.Core.Integrations;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Infrastructure.Integrations;

public sealed class HealthchecksIntegration : HttpIntegration
{
    private const string MissingUrl = "The ping URL is missing";

    public HealthchecksIntegration(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "healthChecks";

    public override IntegrationTypeSchemaDto Schema { get; } = new()
    {
        Name = "healthChecks",
        Title = "Healthchecks.io",
        Description = "Send heartbeats and test results to Healthchecks.io",
        Fields =
        [
            new() { Name = "url", Type = "text", Required = true, Regex = @"https?://.+", Placeholder = "https://hc-ping.com/uuid" },
            new() { Name = "interval", Type = "number", Required = false, Default = 1 }
        ]
    };

    public override Task<IntegrationResult> HandleAsync(IntegrationEvent integrationEvent, IntegrationContext context, CancellationToken cancellationToken)
    {
        var pingUrl = PingUrl(context.Settings);
        if (string.IsNullOrEmpty(pingUrl)) return Task.FromResult(IntegrationResult.Failed(MissingUrl));

        var url = integrationEvent switch
        {
            TestStarted => $"{pingUrl}/start",
            TestFailed => $"{pingUrl}/fail",
            TestSkipped => $"{pingUrl}/log",
            _ => pingUrl
        };

        return PostAsync(url, IntegrationEventData.For(integrationEvent) ?? new { }, cancellationToken);
    }

    public override Task<IntegrationResult> SendTestAsync(IntegrationContext context, Speedtest sample, CancellationToken cancellationToken)
    {
        var pingUrl = PingUrl(context.Settings);
        return string.IsNullOrEmpty(pingUrl)
            ? Task.FromResult(IntegrationResult.Failed(MissingUrl))
            : PostAsync($"{pingUrl}/log", sample, cancellationToken);
    }

    private static string PingUrl(IntegrationSettings settings) => settings.GetString("url").TrimEnd('/');

    private Task<IntegrationResult> PostAsync(string url, object body, CancellationToken cancellationToken)
    {
        var request = JsonPost(url, body);
        request.Headers.Add("User-Agent", "SpeedtestWatcher/HealthAgent");
        return SendAsync(request, cancellationToken);
    }
}
