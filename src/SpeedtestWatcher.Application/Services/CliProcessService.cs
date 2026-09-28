using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.Services;

internal partial class CliProcessService : ICliProcessService
{
    private const int FileNotFound = 2;

    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(120);

    private readonly ILogger<CliProcessService> _logger;

    public CliProcessService(ILogger<CliProcessService> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessOutcome> RunAsync(
        string toolTitle, string binaryPath, IReadOnlyList<string> arguments, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var limit = timeout ?? _defaultTimeout;
        var psi = new ProcessStartInfo
        {
            FileName = binaryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);

        LogStarting(binaryPath, arguments);

        try
        {
            using var process = new Process();
            process.StartInfo = psi;
            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) stdoutBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) stderrBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = new CancellationTokenSource(limit);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill(true);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
                    {
                        LogProcessNotStopped(ex, binaryPath);
                    }
                }

                return new ProcessOutcome(null, timeoutCts.IsCancellationRequested
                    ? $"Speedtest timed out after {limit.TotalSeconds:0} seconds"
                    : "Speedtest was cancelled");
            }

            return new ProcessOutcome(new ToolOutput(stdoutBuilder.ToString(), stderrBuilder.ToString(), process.ExitCode), null);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == FileNotFound)
        {
            return new ProcessOutcome(null, $"The {toolTitle} command-line tool isn't installed. Install it and make sure it's on the PATH.");
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            LogRunFailed(ex, binaryPath);
            return new ProcessOutcome(null, $"{toolTitle} couldn't be started: {ex.Message}");
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Spawning {Binary} with args: {Args}")]
    private partial void LogStarting(string binary, IReadOnlyList<string> args);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not stop the process {Binary}")]
    private partial void LogProcessNotStopped(Exception exception, string binary);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error running {Binary}")]
    private partial void LogRunFailed(Exception exception, string binary);
}
