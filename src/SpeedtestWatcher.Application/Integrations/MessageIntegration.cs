using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

public enum MessageKind
{
    Finished,
    Failed,
    Unhealthy,
    Skipped
}

internal sealed record OutgoingMessage(MessageKind Kind, string Text);

internal abstract class MessageIntegration : HttpIntegration
{
    private static readonly IReadOnlyList<IntegrationFieldSchemaDto> _messageFields =
    [
        new() { Name = "send_finished", Type = "boolean", Required = false, Default = true },
        new() { Name = "finished_message", Type = "textarea", Required = false },
        new() { Name = "send_failed", Type = "boolean", Required = false, Default = true },
        new() { Name = "error_message", Type = "textarea", Required = false },
        new() { Name = "send_unhealthy", Type = "boolean", Required = false, Default = true },
        new() { Name = "unhealthy_message", Type = "textarea", Required = false },
        new() { Name = "send_skipped", Type = "boolean", Required = false, Default = true },
        new() { Name = "skipped_message", Type = "textarea", Required = false }
    ];

    protected MessageIntegration(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    protected abstract string Title { get; }

    protected abstract string Description { get; }

    protected abstract IReadOnlyList<IntegrationFieldSchemaDto> OwnFields { get; }

    protected abstract MessageTemplates Templates { get; }

    public override IntegrationTypeSchemaDto Schema => new()
    {
        Name = Name,
        Title = Title,
        Description = Description,
        Fields = [.. OwnFields, .. _messageFields]
    };

    public override async Task<IntegrationResult> HandleAsync(IntegrationEvent integrationEvent, IntegrationContext context, CancellationToken cancellationToken)
    {
        if (SettingsProblem(context.Settings) is { } problem) return IntegrationResult.Failed(problem);

        MessageKind? kind = integrationEvent switch
        {
            TestFinished => MessageKind.Finished,
            TestFailed => MessageKind.Failed,
            TestUnhealthy => MessageKind.Unhealthy,
            TestSkipped => MessageKind.Skipped,
            _ => null
        };

        if (kind is not { } messageKind || !context.Settings.GetBool(ToggleKey(messageKind), true)) return IntegrationResult.NotApplicable;

        return await SendMessageAsync(Render(messageKind, integrationEvent, context.Settings), context.Settings, cancellationToken);
    }

    public override async Task<IntegrationResult> SendTestAsync(IntegrationContext context, Speedtest sample, CancellationToken cancellationToken)
    {
        if (SettingsProblem(context.Settings) is { } problem) return IntegrationResult.Failed(problem);

        return await SendMessageAsync(Render(MessageKind.Finished, new TestFinished(sample), context.Settings), context.Settings, cancellationToken);
    }

    protected abstract string? SettingsProblem(IntegrationSettings settings);

    protected abstract Task<IntegrationResult> SendMessageAsync(OutgoingMessage message, IntegrationSettings settings, CancellationToken cancellationToken);

    private OutgoingMessage Render(MessageKind kind, IntegrationEvent integrationEvent, IntegrationSettings settings)
    {
        var template = settings.GetString(TemplateKey(kind), Templates.For(kind));
        return new OutgoingMessage(kind, TemplateHelper.ReplaceVariables(template, TemplateVariables.For(integrationEvent)));
    }

    private static string ToggleKey(MessageKind kind) => kind switch
    {
        MessageKind.Finished => "send_finished",
        MessageKind.Failed => "send_failed",
        MessageKind.Unhealthy => "send_unhealthy",
        _ => "send_skipped"
    };

    private static string TemplateKey(MessageKind kind) => kind switch
    {
        MessageKind.Finished => "finished_message",
        MessageKind.Failed => "error_message",
        MessageKind.Unhealthy => "unhealthy_message",
        _ => "skipped_message"
    };
}
