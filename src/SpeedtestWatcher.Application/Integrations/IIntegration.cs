using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

public interface IIntegration
{
    string Name { get; }

    IntegrationTypeSchemaDto Schema { get; }

    Task<IntegrationResult> HandleAsync(IntegrationEvent integrationEvent, IntegrationContext context, CancellationToken cancellationToken);

    Task<IntegrationResult> SendTestAsync(IntegrationContext context, Speedtest sample, CancellationToken cancellationToken);
}

public sealed record IntegrationContext(string Id, IntegrationSettings Settings);

public enum IntegrationOutcome
{
    Sent,
    NotApplicable,
    Failed
}

public sealed record IntegrationResult(IntegrationOutcome Outcome, string? Error = null)
{
    public static IntegrationResult Sent { get; } = new(IntegrationOutcome.Sent);

    public static IntegrationResult NotApplicable { get; } = new(IntegrationOutcome.NotApplicable);

    public static IntegrationResult Failed(string error) => new(IntegrationOutcome.Failed, error);
}
