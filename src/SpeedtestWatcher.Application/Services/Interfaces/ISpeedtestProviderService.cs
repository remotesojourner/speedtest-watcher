using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Utils;

namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface ISpeedtestProviderService
{
    SpeedtestProvider Provider { get; }

    string Title { get; }

    string Description { get; }

    string BinaryName { get; }

    ServerCatalog? Servers { get; }

    string? DownloadUrl(PlatformTarget target);

    ToolArguments BuildArguments(RunOptions options);

    SpeedtestExecutionResult ParseResult(ToolOutput output, RunOptions options);

    async Task<SpeedtestExecutionResult> RunAsync(ICliProcessService processes, string binaryPath, RunOptions options, CancellationToken cancellationToken = default)
    {
        var command = BuildArguments(options);

        if (command.ScratchFileContent != null)
            await File.WriteAllTextAsync(options.ScratchFilePath, command.ScratchFileContent, cancellationToken);

        var outcome = await processes.RunAsync(Title, binaryPath, command.Arguments, cancellationToken: cancellationToken);
        return outcome.Output is { } output ? ParseResult(output, options) : ToolFailure.Because(outcome.FailureMessage!);
    }
}
