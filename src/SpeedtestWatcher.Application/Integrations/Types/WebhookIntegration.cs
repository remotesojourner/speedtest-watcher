using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations.Types;

internal sealed class WebhookIntegration : HttpIntegration
{
    private const string MissingUrl = "The webhook URL is missing";

    public WebhookIntegration(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "webhook";

    public override IntegrationTypeSchemaDto Schema { get; } = new()
    {
        Name = "webhook",
        Title = "Webhook",
        Description = "Generic JSON webhook for custom integrations",
        Fields =
        [
            new() { Name = "url", Type = "text", Required = true, Regex = @"https?://.+", Placeholder = "https://example.com/webhook" },
            new() { Name = "send_started", Type = "boolean", Required = false, Default = false },
            new() { Name = "send_finished", Type = "boolean", Required = false, Default = true },
            new() { Name = "send_alive", Type = "boolean", Required = false, Default = false },
            new() { Name = "send_failed", Type = "boolean", Required = false, Default = true },
            new() { Name = "send_recommendations", Type = "boolean", Required = false, Default = false },
            new() { Name = "send_unhealthy", Type = "boolean", Required = false, Default = true },
            new() { Name = HealthyAgainToggle.Key, Type = "boolean", Required = false, Default = true },
            new() { Name = "send_skipped", Type = "boolean", Required = false, Default = true },
            new() { Name = "send_connection_lost", Type = "boolean", Required = false, Default = true },
            new() { Name = "send_connection_restored", Type = "boolean", Required = false, Default = true },
            new() { Name = "send_config_updates", Type = "boolean", Required = false, Default = false },
            new() { Name = "interval", Type = "number", Required = false, Default = 1 }
        ]
    };

    public override Task<IntegrationResult> SendTestAsync(IntegrationContext context, Speedtest sample, CancellationToken cancellationToken)
    {
        var url = context.Settings.GetString("url");
        return string.IsNullOrEmpty(url)
            ? Task.FromResult(IntegrationResult.Failed(MissingUrl))
            : PostAsync(url, "TEST", sample, cancellationToken);
    }

    public override Task<IntegrationResult> HandleAsync(IntegrationEvent integrationEvent, IntegrationContext context, CancellationToken cancellationToken)
    {
        var settings = context.Settings;
        var url = settings.GetString("url");
        if (string.IsNullOrEmpty(url)) return Task.FromResult(IntegrationResult.Failed(MissingUrl));

        var eventName = integrationEvent switch
        {
            TestStarted when settings.GetBool("send_started") => "TEST_STARTED",
            Heartbeat when settings.GetBool("send_alive") => "KEEP_ALIVE",
            TestFinished when settings.GetBool("send_finished", true) => "TEST_FINISHED",
            TestFailed when settings.GetBool("send_failed", true) => "TEST_FAILED",
            TestUnhealthy when settings.GetBool("send_unhealthy", true) => "TEST_UNHEALTHY",
            TestHealthyAgain when settings.GetBool(HealthyAgainToggle.Key, true) => "TEST_HEALTHY_AGAIN",
            TestSkipped when settings.GetBool("send_skipped", true) => "TEST_SKIPPED",
            ConnectionLost when settings.GetBool("send_connection_lost", true) => "CONNECTION_LOST",
            ConnectionRestored when settings.GetBool("send_connection_restored", true) => "CONNECTION_RESTORED",
            RecommendationsUpdated when settings.GetBool("send_recommendations") => "RECOMMENDATIONS_UPDATED",
            ConfigUpdated when settings.GetBool("send_config_updates") => "CONFIG_UPDATED",
            _ => null
        };

        return eventName == null
            ? Task.FromResult(IntegrationResult.NotApplicable)
            : PostAsync(url, eventName, IntegrationEventData.For(integrationEvent), cancellationToken);
    }

    private Task<IntegrationResult> PostAsync(string url, string eventName, object? data, CancellationToken cancellationToken)
    {
        var request = JsonPost(url, new { @event = eventName, data });
        request.Headers.Add("User-Agent", "SpeedtestWatcher/WebhookAgent");
        return SendAsync(request, cancellationToken);
    }
}
