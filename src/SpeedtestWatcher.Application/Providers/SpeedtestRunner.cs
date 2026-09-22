using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SpeedtestWatcher.Application.Providers;

internal partial class SpeedtestRunner : ISpeedtestRunner
{
    private readonly Dictionary<SpeedtestProvider, ISpeedtestTool> _tools;
    private readonly ICliManager _cliManager;
    private readonly ILogger<SpeedtestRunner> _logger;

    public SpeedtestRunner(IEnumerable<ISpeedtestTool> tools, ICliManager cliManager, ILogger<SpeedtestRunner> logger)
    {
        _tools = tools.ToDictionary(tool => tool.Provider);
        _cliManager = cliManager;
        _logger = logger;
    }

    public async Task<SpeedtestExecutionResult> RunTestAsync(
        SpeedtestProvider provider,
        string? serverId,
        string? customUrl,
        string? networkInterface,
        CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(provider, out var tool))
        {
            return new SpeedtestExecutionResult
            {
                Success = false,
                Error = "No provider selected"
            };
        }

        var binaryPath = _cliManager.GetBinaryPath(provider);
        if (!File.Exists(binaryPath))
        {
            await _cliManager.EnsureBinariesAsync(cancellationToken);
            if (!File.Exists(binaryPath))
            {
                return new SpeedtestExecutionResult
                {
                    Success = false,
                    Error = $"The {tool.Title} command-line tool isn't installed, and it couldn't be downloaded."
                };
            }
        }

        var scratchFile = Path.Combine(Path.GetTempPath(), $"speedtest_watcher_{Guid.NewGuid():N}.json");
        var options = new RunOptions(serverId, customUrl, networkInterface, scratchFile);
        var command = tool.BuildArguments(options);

        try
        {
            if (command.ScratchFileContent != null)
                await File.WriteAllTextAsync(scratchFile, command.ScratchFileContent, cancellationToken);

            var psi = new ProcessStartInfo
            {
                FileName = binaryPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in command.Arguments)
                psi.ArgumentList.Add(argument);

            LogStarting(binaryPath, command.Arguments);

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

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
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
                        LogProcessNotStopped(ex, provider);
                    }
                }
                return new SpeedtestExecutionResult
                {
                    Success = false,
                    Error = timeoutCts.IsCancellationRequested ? "Speedtest timed out after 120 seconds" : "Speedtest was cancelled"
                };
            }

            return ReadResult(tool, new ToolOutput(stdoutBuilder.ToString(), stderrBuilder.ToString(), process.ExitCode), options);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            LogRunFailed(ex, provider);
            return ToolFailure.Because($"{tool.Title} couldn't be started: {ex.Message}");
        }
        finally
        {
            if (File.Exists(scratchFile))
            {
                try
                {
                    File.Delete(scratchFile);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    LogConfigNotDeleted(ex, scratchFile);
                }
            }
        }
    }

    private SpeedtestExecutionResult ReadResult(ISpeedtestTool tool, ToolOutput output, RunOptions options)
    {
        try
        {
            return tool.ParseResult(output, options);
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or KeyNotFoundException)
        {
            LogResultUnreadable(ex, tool.Provider);
            return ToolFailure.Because($"{tool.Title} printed a result that couldn't be read.");
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The {Provider} result couldn't be read")]
    private partial void LogResultUnreadable(Exception exception, SpeedtestProvider provider);

    [LoggerMessage(Level = LogLevel.Information, Message = "Spawning {Binary} with args: {Args}")]
    private partial void LogStarting(string binary, IReadOnlyList<string> args);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not stop the {Provider} speedtest process")]
    private partial void LogProcessNotStopped(Exception exception, SpeedtestProvider provider);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error running speedtest for {Provider}")]
    private partial void LogRunFailed(Exception exception, SpeedtestProvider provider);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not delete the temporary speedtest config {Path}")]
    private partial void LogConfigNotDeleted(Exception exception, string path);
}
