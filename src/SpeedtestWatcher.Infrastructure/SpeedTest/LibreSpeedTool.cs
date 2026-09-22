using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.SpeedTest;

namespace SpeedtestWatcher.Infrastructure.SpeedTest;

public sealed class LibreSpeedTool : ISpeedtestTool
{
    private const string DownloadBase = "https://github.com/librespeed/speedtest-cli/releases/download/v1.0.10/librespeed-cli_1.0.10_";

    private static readonly Dictionary<PlatformTarget, string> Downloads = new()
    {
        [new(OSPlatform.Windows, Architecture.X64)] = "windows_amd64.zip",
        [new(OSPlatform.Windows, Architecture.Arm64)] = "windows_arm64.zip",
        [new(OSPlatform.OSX, Architecture.X64)] = "darwin_amd64.tar.gz",
        [new(OSPlatform.OSX, Architecture.Arm64)] = "darwin_arm64.tar.gz",
        [new(OSPlatform.Linux, Architecture.X64)] = "linux_amd64.tar.gz",
        [new(OSPlatform.Linux, Architecture.Arm64)] = "linux_arm64.tar.gz",
        [new(OSPlatform.Linux, Architecture.Arm)] = "linux_armv7.tar.gz"
    };

    public SpeedtestProvider Provider => SpeedtestProvider.Libre;

    public string Title => "LibreSpeed";

    public string Description => "Open-source, self-hostable speedtest";

    public string BinaryName => "librespeed-cli";

    public ServerCatalog? Servers { get; } = new("https://librespeed.org/backend-servers/servers.php", ParseServers);

    public string? DownloadUrl(PlatformTarget target) => Downloads.TryGetValue(target, out var file) ? DownloadBase + file : null;

    public ToolArguments BuildArguments(RunOptions options)
    {
        var arguments = new List<string> { "--json", "--duration=5" };

        if (!string.IsNullOrEmpty(options.NetworkInterface))
            arguments.Add($"--source={options.NetworkInterface}");

        if (!string.IsNullOrEmpty(options.CustomServerUrl))
        {
            arguments.Add($"--local-json={options.ScratchFilePath}");
            arguments.Add("--server=1");
            return new ToolArguments(arguments, CustomServerList(options.CustomServerUrl));
        }

        if (!string.IsNullOrEmpty(options.ServerId))
            arguments.Add($"--server={options.ServerId}");

        return new ToolArguments(arguments);
    }

    public SpeedtestExecutionResult? ParseResult(string output) =>
        JsonOutput.FromLastLine(output, root => Parse(JsonOutput.FirstIfArray(root)));

    public static SpeedtestExecutionResult Parse(JsonElement root)
    {
        var result = new SpeedtestExecutionResult { Success = true };

        if (root.TryGetProperty("ping", out var ping))
            result.Ping = (int)Math.Round(ping.GetDouble());

        if (root.TryGetProperty("jitter", out var jitter))
        {
            if (jitter.ValueKind == JsonValueKind.Number)
                result.Jitter = Math.Round(jitter.GetDouble(), 2);
            else if (jitter.ValueKind == JsonValueKind.String
                     && double.TryParse(jitter.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                result.Jitter = Math.Round(parsed, 2);
        }

        if (root.TryGetProperty("download", out var download))
            result.Download = Math.Round(download.GetDouble(), 2);

        if (root.TryGetProperty("upload", out var upload))
            result.Upload = Math.Round(upload.GetDouble(), 2);

        if (root.TryGetProperty("elapsed", out var elapsed))
            result.Time = (int)Math.Round(elapsed.GetDouble() / 1000.0);

        if (root.TryGetProperty("server", out var server))
        {
            if (server.TryGetProperty("id", out var id))
                result.ServerId = id.GetInt32();
            if (server.TryGetProperty("name", out var name))
                result.ServerName = name.GetString();
            if (server.TryGetProperty("url", out var url))
                result.ServerHost = url.GetString();
        }

        return result;
    }

    private static string CustomServerList(string url) => JsonSerializer.Serialize(new[]
    {
        new
        {
            id = 1,
            name = "Custom Server",
            server = url,
            dlURL = "garbage.php",
            ulURL = "empty.php",
            pingURL = "empty.php",
            getIpURL = "getIP.php"
        }
    });

    private static IReadOnlyList<ServerInfo> ParseServers(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array) return [];

        return document.RootElement.EnumerateArray()
            .Where(row => JsonOutput.Text(row, "id") != null)
            .Select(row => new ServerInfo(
                JsonOutput.Text(row, "id")!,
                JsonOutput.Text(row, "name") ?? JsonOutput.Text(row, "id")!,
                Host: JsonOutput.Text(row, "server")))
            .ToList();
    }
}
