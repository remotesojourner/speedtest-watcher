using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpeedtestWatcher.Application.Installers;
using SpeedtestWatcher.Application.Repositories.Interfaces;
using SpeedtestWatcher.Application.Services;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.TestSupport;

internal static class TestIntegrations
{
    public static IntegrationDispatchService Dispatcher(IIntegrationRepository repository, HttpMessageHandler handler, ILogger<IntegrationDispatchService>? logger = null) =>
        new(repository, All(handler), new HeartbeatScheduleService(), logger ?? NullLogger<IntegrationDispatchService>.Instance);

    public static IReadOnlyList<IIntegrationTypeService> All(HttpMessageHandler handler) =>
        new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(handler))
            .AddIntegrations()
            .BuildServiceProvider()
            .GetServices<IIntegrationTypeService>()
            .ToList();
}
