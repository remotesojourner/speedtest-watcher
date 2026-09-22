using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpeedtestWatcher.Application.Providers;

internal sealed partial class OoklaTool : ISpeedtestTool
{
    private const string DownloadBase = "https://install.speedtest.net/app/cli/ookla-speedtest-1.2.0-";

    private static readonly Dictionary<PlatformTarget, string> _downloads = new()
    {
        [new(OSPlatform.Windows, Architecture.X64)] = "win64.zip",
        [new(OSPlatform.OSX, Architecture.X64)] = "macosx-x86_64.tgz",
        [new(OSPlatform.Linux, Architecture.X64)] = "linux-x86_64.tgz",
        [new(OSPlatform.Linux, Architecture.Arm64)] = "linux-aarch64.tgz",
        [new(OSPlatform.Linux, Architecture.Arm)] = "linux-armhf.tgz",
        [new(OSPlatform.Linux, Architecture.X86)] = "linux-i386.tgz"
    };

    public SpeedtestProvider Provider => SpeedtestProvider.Ookla;

    public string Title => "Ookla";

    public string Description => "Popular provider with a global server network";

    public string BinaryName => "speedtest";

    public ServerCatalog? Servers { get; } = new("https://www.speedtest.net/api/js/servers?limit=20", ParseServers);

    public string? DownloadUrl(PlatformTarget target) => _downloads.TryGetValue(target, out var file) ? DownloadBase + file : null;

    public ToolArguments BuildArguments(RunOptions options)
    {
        var arguments = new List<string> { "--accept-license", "--accept-gdpr", "--format=json" };

        if (!string.IsNullOrEmpty(options.NetworkInterface))
            arguments.Add(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"--ip={options.NetworkInterface}" : $"--interface={options.NetworkInterface}");

        if (!string.IsNullOrEmpty(options.ServerId))
            arguments.Add($"--server-id={options.ServerId}");

        return new ToolArguments(arguments);
    }

    public SpeedtestExecutionResult ParseResult(ToolOutput output, RunOptions options)
    {
        if (JsonOutput.FromLastLine(output.Output, root => JsonOutput.FirstIfArray(root) is var line && IsResult(line) ? Parse(line) : null) is { } result)
            return result;

        var errors = output.ErrorLines.Concat(output.OutputLines).Select(ErrorMessage).OfType<string>().Distinct().ToList();

        if (errors.Any(error => error.Contains("NoServersException", StringComparison.Ordinal)))
        {
            return ToolFailure.Because(options.ServerId is { } serverId
                ? $"Ookla has no server {serverId}. Pick one from its server list, or check the ID on speedtest.net."
                : "Ookla found no server to test against.");
        }

        if (options.NetworkInterface is { } networkInterface && errors.Any(error => error.Contains("Failed binding local connection end", StringComparison.Ordinal)))
            return ToolFailure.Because($"Ookla couldn't send traffic through the network interface {networkInterface}.");

        return errors.Count > 0
            ? ToolFailure.Because($"Ookla couldn't run the test: {Readable(errors[0])}.")
            : ToolFailure.Unrecognised(Title, output);
    }

    public static SpeedtestExecutionResult Parse(JsonElement root)
    {
        var result = new SpeedtestExecutionResult { Success = true };

        if (root.TryGetProperty("ping", out var ping))
        {
            if (ping.TryGetProperty("latency", out var latency))
                result.Ping = (int)Math.Round(latency.GetDouble());
            if (ping.TryGetProperty("jitter", out var jitter))
                result.Jitter = Math.Round(jitter.GetDouble(), 2);
        }

        double downloadElapsed = 0, uploadElapsed = 0;
        if (root.TryGetProperty("download", out var download))
        {
            if (download.TryGetProperty("bandwidth", out var bandwidth))
                result.Download = Megabits(bandwidth.GetDouble());
            if (download.TryGetProperty("elapsed", out var elapsed))
                downloadElapsed = elapsed.GetDouble();
        }

        if (root.TryGetProperty("upload", out var upload))
        {
            if (upload.TryGetProperty("bandwidth", out var bandwidth))
                result.Upload = Megabits(bandwidth.GetDouble());
            if (upload.TryGetProperty("elapsed", out var elapsed))
                uploadElapsed = elapsed.GetDouble();
        }

        result.Time = (int)Math.Round((downloadElapsed + uploadElapsed) / 1000.0);

        if (root.TryGetProperty("server", out var server))
        {
            if (server.TryGetProperty("id", out var id))
                result.ServerId = id.GetInt32();
            if (server.TryGetProperty("name", out var name))
                result.ServerName = name.GetString();
            if (server.TryGetProperty("host", out var host))
                result.ServerHost = host.GetString();
        }

        if (root.TryGetProperty("result", out var resultElement) && resultElement.TryGetProperty("id", out var resultId))
            result.ResultId = resultId.GetString();

        return result;
    }

    private static string? ErrorMessage(string line)
    {
        if (PlainErrorLine().Match(line) is { Success: true } plain) return plain.Groups["message"].Value;
        if (!line.StartsWith('{')) return null;

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            return JsonOutput.Text(root, "type") == "log" && JsonOutput.Text(root, "level") == "error" ? JsonOutput.Text(root, "message") : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Readable(string error) => OoklaErrorParts().Match(error) is { Success: true } parts ? parts.Groups["text"].Value : error;

    [GeneratedRegex(@"^\[[^\]]*\] \[error\] (?<message>.+)$")]
    private static partial Regex PlainErrorLine();

    [GeneratedRegex(@"^(?:\w+ - )?(?<text>.+?)(?: \(\w+\))?$")]
    private static partial Regex OoklaErrorParts();

    private static bool IsResult(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty("type", out var type)
        && type.ValueKind == JsonValueKind.String
        && type.GetString() == "result";

    private static double Megabits(double bytesPerSecond) => Math.Round(bytesPerSecond / 1250.0, 2) / 100.0;

    private static List<ServerInfo> ParseServers(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array) return [];

        return document.RootElement.EnumerateArray()
            .Where(row => JsonOutput.Text(row, "id") != null)
            .Select(row => new ServerInfo(
                JsonOutput.Text(row, "id")!,
                JsonOutput.Text(row, "name") ?? JsonOutput.Text(row, "id")!,
                JsonOutput.Text(row, "sponsor"),
                JsonOutput.Text(row, "country"),
                JsonOutput.Number(row, "distance"),
                JsonOutput.Text(row, "host")))
            .ToList();
    }
}
