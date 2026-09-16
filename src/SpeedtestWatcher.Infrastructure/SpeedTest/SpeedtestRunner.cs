using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Infrastructure.SpeedTest;

public class SpeedtestRunner : ISpeedtestRunner
{
    private readonly ICliManager _cliManager;
    private readonly ILogger<SpeedtestRunner> _logger;

    public SpeedtestRunner(ICliManager cliManager, ILogger<SpeedtestRunner> logger)
    {
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
        if (provider == SpeedtestProvider.None)
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
                    Error = $"CLI binary for {provider} not found."
                };
            }
        }

        var args = new List<string>();
        string? tempConfigPath = null;

        if (provider == SpeedtestProvider.Ookla)
        {
            args.Add("--accept-license");
            args.Add("--accept-gdpr");
            args.Add("--format=json");

            if (!string.IsNullOrEmpty(networkInterface) && networkInterface != "none")
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    args.Add($"--ip={networkInterface}");
                else
                    args.Add($"--interface={networkInterface}");
            }

            if (!string.IsNullOrEmpty(serverId) && serverId != "none")
                args.Add($"--server-id={serverId}");
        }
        else if (provider == SpeedtestProvider.Libre)
        {
            args.Add("--json");
            args.Add("--duration=5");

            if (!string.IsNullOrEmpty(networkInterface) && networkInterface != "none")
                args.Add($"--source={networkInterface}");

            if (!string.IsNullOrEmpty(customUrl) && customUrl != "none")
            {
                var customServer = new[]
                {
                    new
                    {
                        id = 1,
                        name = "Custom Server",
                        server = customUrl,
                        dlURL = "garbage.php",
                        ulURL = "empty.php",
                        pingURL = "empty.php",
                        getIpURL = "getIP.php"
                    }
                };

                tempConfigPath = Path.Combine(Path.GetTempPath(), $"libre_custom_{Guid.NewGuid()}.json");
                await File.WriteAllTextAsync(tempConfigPath, JsonSerializer.Serialize(customServer), cancellationToken);
                args.Add($"--local-json={tempConfigPath}");
                args.Add("--server=1");
            }
            else if (!string.IsNullOrEmpty(serverId) && serverId != "none")
            {
                args.Add($"--server={serverId}");
            }
        }
        else if (provider == SpeedtestProvider.Cloudflare)
        {
            args.Add("--output-format=json");

            if (!string.IsNullOrEmpty(networkInterface) && networkInterface != "none")
            {
                if (networkInterface.Contains(':'))
                    args.Add($"--ipv6={networkInterface}");
                else
                    args.Add($"--ipv4={networkInterface}");
            }
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = binaryPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var a in args)
                psi.ArgumentList.Add(a);

            _logger.LogInformation("Spawning {Binary} with args: {Args}", binaryPath, string.Join(" ", args));

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
                        _logger.LogWarning(ex, "Could not stop the {Provider} speedtest process", provider);
                    }
                }
                return new SpeedtestExecutionResult
                {
                    Success = false,
                    Error = timeoutCts.IsCancellationRequested ? "Speedtest timed out after 120 seconds" : "Speedtest was cancelled"
                };
            }

            var stdout = stdoutBuilder.ToString().Trim();
            var stderr = stderrBuilder.ToString().Trim();

            if (!string.IsNullOrEmpty(stderr) && stderr.Contains("Too many requests", StringComparison.OrdinalIgnoreCase))
            {
                return new SpeedtestExecutionResult
                {
                    Success = false,
                    Error = "Too many requests. Please try again later"
                };
            }

            if (string.IsNullOrWhiteSpace(stdout))
            {
                return new SpeedtestExecutionResult
                {
                    Success = false,
                    Error = !string.IsNullOrEmpty(stderr) ? stderr : "No output from speedtest binary"
                };
            }

            using var resultDocument = FindResultDocument(stdout, provider);
            if (resultDocument != null)
            {
                return OutputParser.Parse(provider, ResultElement(resultDocument, provider));
            }

            return new SpeedtestExecutionResult
            {
                Success = false,
                Error = !string.IsNullOrEmpty(stderr) ? stderr : "Failed to parse speedtest output"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running speedtest for {Provider}", provider);
            return new SpeedtestExecutionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
        finally
        {
            if (tempConfigPath != null && File.Exists(tempConfigPath))
            {
                try
                {
                    File.Delete(tempConfigPath);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogWarning(ex, "Could not delete the temporary speedtest config {Path}", tempConfigPath);
                }
            }
        }
    }

    private static JsonDocument? FindResultDocument(string stdout, SpeedtestProvider provider)
    {
        var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).Reverse();
        foreach (var line in lines)
        {
            if (!line.StartsWith('{') && !line.StartsWith('[')) continue;

            var document = TryParseJson(line);
            if (document == null) continue;

            if (provider != SpeedtestProvider.Ookla || IsOoklaResult(ResultElement(document, provider)))
                return document;

            document.Dispose();
        }

        return null;
    }

    private static JsonDocument? TryParseJson(string line)
    {
        try
        {
            return JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonElement ResultElement(JsonDocument document, SpeedtestProvider provider)
    {
        var root = document.RootElement;
        var isWrappedInArray = root.ValueKind == JsonValueKind.Array && provider != SpeedtestProvider.Cloudflare && root.GetArrayLength() > 0;
        return isWrappedInArray ? root[0] : root;
    }

    private static bool IsOoklaResult(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty("type", out var type)
        && type.ValueKind == JsonValueKind.String
        && type.GetString() == "result";
}
