using Microsoft.Extensions.Diagnostics.HealthChecks;
using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.Web.Startup;

internal sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopes;

    public DatabaseHealthCheck(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IStorageRepository>();
            return await storage.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("The database answered")
                : HealthCheckResult.Unhealthy("The database didn't answer");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("The database didn't answer", ex);
        }
    }
}
