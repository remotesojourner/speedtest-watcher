using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.IntegrationTests.Application.Common;

public class BackgroundServiceShutdownTests
{
    [Theory]
    [InlineData(typeof(RetentionCleanupService))]
    [InlineData(typeof(IntegrationTickerService))]
    [InlineData(typeof(InterfaceRefreshService))]
    public async Task StoppingTheHost_EndsTheLoopWithoutThrowing(Type serviceType)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var warnings = new WarningSignal();
        var provider = new ServiceCollection().AddLogging(logging => logging.AddProvider(warnings)).BuildServiceProvider();
        var service = (BackgroundService)ActivatorUtilities.CreateInstance(provider, serviceType);

        await service.StartAsync(cancellationToken);
        await warnings.FirstWarning.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        await service.StopAsync(cancellationToken);

        Assert.Equal(TaskStatus.RanToCompletion, service.ExecuteTask!.Status);
    }

    private sealed class WarningSignal : ILoggerProvider, ILogger
    {
        private readonly TaskCompletionSource _firstWarning = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task FirstWarning => _firstWarning.Task;

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) _firstWarning.TrySetResult();
        }

        public void Dispose()
        {
        }
    }
}
