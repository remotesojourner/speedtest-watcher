using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.Services;

internal partial class CliBinaryService : ICliBinaryService
{
    private readonly IReadOnlyDictionary<SpeedtestProvider, ISpeedtestProviderService> _tools;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CliBinaryService> _logger;
    private readonly string _binDirectory;

    public CliBinaryService(IEnumerable<ISpeedtestProviderService> tools, IHttpClientFactory httpClientFactory, IOptions<SpeedtestWatcherOptions> options, ILogger<CliBinaryService> logger)
    {
        _tools = tools.ToDictionary(tool => tool.Provider);
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _binDirectory = options.Value.BinDirectory;
    }

    public string GetBinaryPath(SpeedtestProvider provider)
    {
        if (!_tools.TryGetValue(provider, out var tool)) throw new ArgumentOutOfRangeException(nameof(provider));

        var binDirectoryPath = BinaryPath(tool);
        return File.Exists(binDirectoryPath) || tool.DownloadUrl(PlatformTarget.Current) != null ? binDirectoryPath : FileName(tool);
    }

    public bool IsBinaryAvailable(SpeedtestProvider provider) =>
        _tools.TryGetValue(provider, out var tool) && File.Exists(BinaryPath(tool));

    public async Task EnsureBinariesAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_binDirectory);

        foreach (var tool in _tools.Values.Where(tool => !File.Exists(BinaryPath(tool)) && tool.DownloadUrl(PlatformTarget.Current) != null))
        {
            try
            {
                LogDownloading(tool.Provider);
                await DownloadBinaryAsync(tool, cancellationToken);
                LogInstalled(tool.Provider);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException or InvalidDataException
                                       || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                LogDownloadFailed(ex, tool.Provider);
            }
        }
    }

    private string BinaryPath(ISpeedtestProviderService tool) => Path.Combine(_binDirectory, FileName(tool));

    private static string FileName(ISpeedtestProviderService tool) =>
        tool.BinaryName + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");

    private async Task DownloadBinaryAsync(ISpeedtestProviderService tool, CancellationToken cancellationToken)
    {
        var url = tool.DownloadUrl(PlatformTarget.Current);
        if (string.IsNullOrEmpty(url))
        {
            LogNoBinaryForPlatform(tool.Provider);
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
                    LogDownloadNotDeleted(ex, tempFile);
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
                LogFileModeNotSet(ex, path);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Downloading speedtest CLI for {Provider}...")]
    private partial void LogDownloading(SpeedtestProvider provider);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully installed {Provider} CLI")]
    private partial void LogInstalled(SpeedtestProvider provider);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to download binary for {Provider}")]
    private partial void LogDownloadFailed(Exception exception, SpeedtestProvider provider);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No compatible binary URL found for {Provider} on this platform")]
    private partial void LogNoBinaryForPlatform(SpeedtestProvider provider);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not delete the downloaded file {Path}")]
    private partial void LogDownloadNotDeleted(Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to set unix file mode on {Path}")]
    private partial void LogFileModeNotSet(Exception exception, string path);
}
