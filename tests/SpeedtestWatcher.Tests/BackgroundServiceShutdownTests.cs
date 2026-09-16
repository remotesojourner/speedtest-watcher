using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Web.Background;

namespace SpeedtestWatcher.Tests;

public class BackgroundServiceShutdownTests
{
    [Theory]
    [InlineData(typeof(RetentionCleanupService))]
    [InlineData(typeof(IntegrationTickerService))]
    [InlineData(typeof(InterfaceRefreshService))]
    public async Task StoppingTheHost_EndsTheLoopWithoutThrowing(Type serviceType)
    {
        // No repositories are registered, so the first tick fails and logs a warning right before the loop's delay.
        var warnings = new WarningSignal();
        var provider = new ServiceCollection().AddLogging(logging => logging.AddProvider(warnings)).BuildServiceProvider();
        var service = (BackgroundService)ActivatorUtilities.CreateInstance(provider, serviceType);

        await service.StartAsync(CancellationToken.None);
        // ExecuteAsync starts in the background; stopping before the first tick would skip the delay and pass for the wrong reason.
        await warnings.FirstWarning.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        // A cancellation that escapes ExecuteAsync leaves the task Canceled, and the debugger reports it as user-unhandled.
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
