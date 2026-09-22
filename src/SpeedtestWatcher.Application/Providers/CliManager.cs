using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Providers;

internal class CliManager : ICliManager
{
    private readonly IReadOnlyDictionary<SpeedtestProvider, ISpeedtestTool> _tools;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CliManager> _logger;
    private readonly string _binDirectory;

    public CliManager(IEnumerable<ISpeedtestTool> tools, IHttpClientFactory httpClientFactory, IOptions<SpeedtestWatcherOptions> options, ILogger<CliManager> logger)
    {
        _tools = tools.ToDictionary(tool => tool.Provider);
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _binDirectory = options.Value.BinDirectory;
    }

    public string GetBinaryPath(SpeedtestProvider provider) =>
        _tools.TryGetValue(provider, out var tool) ? BinaryPath(tool) : throw new ArgumentOutOfRangeException(nameof(provider));

    public bool IsBinaryAvailable(SpeedtestProvider provider) =>
        _tools.TryGetValue(provider, out var tool) && File.Exists(BinaryPath(tool));

    public async Task EnsureBinariesAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_binDirectory);

        foreach (var tool in _tools.Values.Where(tool => !File.Exists(BinaryPath(tool))))
        {
            try
            {
                _logger.LogInformation("Downloading speedtest CLI for {Provider}...", tool.Provider);
                await DownloadBinaryAsync(tool, cancellationToken);
                _logger.LogInformation("Successfully installed {Provider} CLI", tool.Provider);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException or InvalidDataException
                                       || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                _logger.LogWarning(ex, "Failed to download binary for {Provider}", tool.Provider);
            }
        }
    }

    private string BinaryPath(ISpeedtestTool tool) =>
        Path.Combine(_binDirectory, tool.BinaryName + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));

    private async Task DownloadBinaryAsync(ISpeedtestTool tool, CancellationToken cancellationToken)
    {
        var url = tool.DownloadUrl(PlatformTarget.Current);
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("No compatible binary URL found for {Provider} on this platform", tool.Provider);
            return;
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"speedtest_watcher_{Guid.NewGuid()}_{Path.GetFileName(url)}");
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(3);

            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs, cancellationToken);
            }

            ExtractBinary(tempFile, BinaryPath(tool));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogWarning(ex, "Could not delete the downloaded file {Path}", tempFile);
                }
            }
        }
    }

    private void ExtractBinary(string archivePath, string targetPath)
    {
        var targetFileName = Path.GetFileName(targetPath);

        if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var archive = ZipFile.OpenRead(archivePath);
            foreach (var entry in archive.Entries)
            {
                if (entry.Name.Equals(targetFileName, StringComparison.OrdinalIgnoreCase) ||
                    entry.Name.Equals(Path.GetFileNameWithoutExtension(targetFileName), StringComparison.OrdinalIgnoreCase))
                {
                    entry.ExtractToFile(targetPath, true);
                    SetExecutablePermissions(targetPath);
                    return;
                }
            }
        }
        else
        {
            using var fileStream = File.OpenRead(archivePath);
            using var gzStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var tarReader = new System.Formats.Tar.TarReader(gzStream);
            while (tarReader.GetNextEntry() is { } entry)
            {
                if (entry.EntryType is System.Formats.Tar.TarEntryType.RegularFile or System.Formats.Tar.TarEntryType.V7RegularFile)
                {
                    var entryName = Path.GetFileName(entry.Name);
                    if (entryName.Equals(targetFileName, StringComparison.OrdinalIgnoreCase) ||
                        entryName.Equals(Path.GetFileNameWithoutExtension(targetFileName), StringComparison.OrdinalIgnoreCase))
                    {
                        entry.ExtractToFile(targetPath, true);
                        SetExecutablePermissions(targetPath);
                        return;
                    }
                }
            }
        }
    }

    private void SetExecutablePermissions(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                File.SetUnixFileMode(path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                _logger.LogDebug(ex, "Failed to set unix file mode on {Path}", path);
            }
        }
    }
}
