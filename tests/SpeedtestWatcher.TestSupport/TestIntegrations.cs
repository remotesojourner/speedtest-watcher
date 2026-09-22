using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpeedtestWatcher.Application.Integrations;

namespace SpeedtestWatcher.TestSupport;

internal static class TestIntegrations
{
    public static IntegrationDispatcher Dispatcher(IIntegrationRepository repository, HttpMessageHandler handler, ILogger<IntegrationDispatcher>? logger = null) =>
        new(repository, All(handler), new HeartbeatSchedule(), logger ?? NullLogger<IntegrationDispatcher>.Instance);

    public static IReadOnlyList<IIntegration> All(HttpMessageHandler handler) =>
        new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(handler))
            .AddIntegrations()
            .BuildServiceProvider()
            .GetServices<IIntegration>()
            .ToList();
}
