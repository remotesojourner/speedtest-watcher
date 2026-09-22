using System.Runtime.InteropServices;
using System.Text.Json;

namespace SpeedtestWatcher.Application.Providers;

internal sealed class CloudflareTool : ISpeedtestTool
{
    private const string DownloadBase = "https://github.com/code-inflation/cfspeedtest/releases/download/v2.2.2/";
    private const string MetadataFailure = "Error fetching metadata: ";
    private const int DurationWhenUnreported = 30;

    private static readonly Dictionary<PlatformTarget, string> _downloads = new()
    {
        [new(OSPlatform.Windows, Architecture.X64)] = "cfspeedtest-x86_64-pc-windows-msvc.zip",
        [new(OSPlatform.OSX, Architecture.X64)] = "cfspeedtest-x86_64-apple-darwin.tar.gz",
        [new(OSPlatform.OSX, Architecture.Arm64)] = "cfspeedtest-aarch64-apple-darwin.tar.gz",
        [new(OSPlatform.Linux, Architecture.X64)] = "cfspeedtest-x86_64-unknown-linux-gnu.tar.gz",
        [new(OSPlatform.Linux, Architecture.Arm64)] = "cfspeedtest-aarch64-unknown-linux-gnu.tar.gz"
    };

    public SpeedtestProvider Provider => SpeedtestProvider.Cloudflare;

    public string Title => "Cloudflare";

    public string Description => "Fast CDN-based testing";

    public string BinaryName => "cfspeedtest";

    public ServerCatalog? Servers => null;

    public string? DownloadUrl(PlatformTarget target) => _downloads.TryGetValue(target, out var file) ? DownloadBase + file : null;

    public ToolArguments BuildArguments(RunOptions options)
    {
        var arguments = new List<string> { "--output-format=json" };

        if (!string.IsNullOrEmpty(options.NetworkInterface))
            arguments.Add(options.NetworkInterface.Contains(':') ? $"--ipv6={options.NetworkInterface}" : $"--ipv4={options.NetworkInterface}");

        return new ToolArguments(arguments);
    }

    public SpeedtestExecutionResult ParseResult(ToolOutput output, RunOptions options)
    {
        if (JsonOutput.FromLastLine(output.Output, root => IsResult(root) ? Parse(root) : null) is { } result)
            return result;

        if (output.ErrorLines.Any(line => line.StartsWith(MetadataFailure, StringComparison.Ordinal)))
        {
            return ToolFailure.Because(options.NetworkInterface is { } networkInterface
                ? $"Cloudflare couldn't reach speed.cloudflare.com through the network interface {networkInterface}."
                : "Cloudflare couldn't reach speed.cloudflare.com. Check the internet connection.");
        }

        return ToolFailure.Unrecognised(Title, output);
    }

    public static SpeedtestExecutionResult Parse(JsonElement root)
    {
        var result = new SpeedtestExecutionResult { Success = true };

        if (root.TryGetProperty("speed_measurements", out var measurements) && measurements.ValueKind == JsonValueKind.Array)
        {
            var downloads = new List<double>();
            var uploads = new List<double>();

            foreach (var item in measurements.EnumerateArray())
            {
                var type = item.TryGetProperty("test_type", out var testType) ? testType.GetString() : null;
                double speed = 0;
                if (item.TryGetProperty("max", out var max)) speed = max.GetDouble();
                else if (item.TryGetProperty("median", out var median)) speed = median.GetDouble();

                if (type == "Download") downloads.Add(speed);
                else if (type == "Upload") uploads.Add(speed);
            }

            result.Download = downloads.Count > 0 ? Math.Round(downloads.Max(), 2) : 0;
            result.Upload = uploads.Count > 0 ? Math.Round(uploads.Max(), 2) : 0;
        }

        if (root.TryGetProperty("latency_measurement", out var latency))
        {
            if (latency.TryGetProperty("avg_latency_ms", out var average))
                result.Ping = (int)Math.Round(average.GetDouble());

            if (latency.TryGetProperty("latency_measurements", out var samples) && samples.ValueKind == JsonValueKind.Array)
                result.Jitter = Jitter(samples.EnumerateArray().Select(sample => sample.GetDouble()).ToList());
        }

        result.Time = root.TryGetProperty("elapsed", out var elapsed)
            ? (int)Math.Round(elapsed.GetDouble() / 1000.0)
            : DurationWhenUnreported;

        return result;
    }

    private static bool IsResult(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty("speed_measurements", out _);

    private static double? Jitter(List<double> latencies)
    {
        if (latencies.Count < 2) return null;

        double totalDifference = 0;
        for (var i = 1; i < latencies.Count; i++)
            totalDifference += Math.Abs(latencies[i] - latencies[i - 1]);

        return Math.Round(totalDifference / (latencies.Count - 1), 2);
    }
}
