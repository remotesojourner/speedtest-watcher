using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Application.Utils;

namespace SpeedtestWatcher.Application.Services;

internal partial class ToolRunnerService : IToolRunnerService
{
    private readonly Dictionary<SpeedtestProvider, ISpeedtestProviderService> _tools;
    private readonly ICliBinaryService _cliManager;
    private readonly ICliProcessService _processes;
    private readonly ILogger<ToolRunnerService> _logger;

    public ToolRunnerService(IEnumerable<ISpeedtestProviderService> tools, ICliBinaryService cliManager, ICliProcessService processes, ILogger<ToolRunnerService> logger)
    {
        _tools = tools.ToDictionary(tool => tool.Provider);
        _cliManager = cliManager;
        _processes = processes;
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
        if (tool.DownloadUrl(PlatformTarget.Current) != null && !File.Exists(binaryPath))
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

        try
        {
            return await tool.RunAsync(_processes, binaryPath, options, cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or KeyNotFoundException)
        {
            LogResultUnreadable(ex, provider);
            return ToolFailure.Because($"{tool.Title} printed a result that couldn't be read.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogRunFailed(ex, provider);
            return ToolFailure.Because($"{tool.Title} couldn't be started: {ex.Message}");
        }
        finally
        {
            DeleteScratchFile(scratchFile);
        }
    }

    private void DeleteScratchFile(string path)
    {
        if (!File.Exists(path)) return;

        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogConfigNotDeleted(ex, path);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The {Provider} result couldn't be read")]
    private partial void LogResultUnreadable(Exception exception, SpeedtestProvider provider);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error running speedtest for {Provider}")]
    private partial void LogRunFailed(Exception exception, SpeedtestProvider provider);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not delete the temporary speedtest config {Path}")]
    private partial void LogConfigNotDeleted(Exception exception, string path);
}
