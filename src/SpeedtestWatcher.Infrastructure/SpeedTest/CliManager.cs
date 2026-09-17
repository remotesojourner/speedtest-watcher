using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Hosting;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Infrastructure.SpeedTest;

public class CliManager : ICliManager
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CliManager> _logger;
    private readonly string _binDirectory;

    public CliManager(IHttpClientFactory httpClientFactory, IOptions<SpeedtestWatcherOptions> options, ILogger<CliManager> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _binDirectory = options.Value.BinDirectory;
    }

    public string GetBinaryPath(SpeedtestProvider provider)
    {
        var exeSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
        var name = provider switch
        {
            SpeedtestProvider.Ookla => $"speedtest{exeSuffix}",
            SpeedtestProvider.Libre => $"librespeed-cli{exeSuffix}",
            SpeedtestProvider.Cloudflare => $"cfspeedtest{exeSuffix}",
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };
        return Path.Combine(_binDirectory, name);
    }

    public bool IsBinaryAvailable(SpeedtestProvider provider) =>
        provider is SpeedtestProvider.Ookla or SpeedtestProvider.Libre or SpeedtestProvider.Cloudflare
        && File.Exists(GetBinaryPath(provider));

    public async Task EnsureBinariesAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_binDirectory);

        foreach (var provider in new[] { SpeedtestProvider.Ookla, SpeedtestProvider.Libre, SpeedtestProvider.Cloudflare })
        {
            if (!IsBinaryAvailable(provider))
            {
                try
                {
                    _logger.LogInformation("Downloading speedtest CLI for {Provider}...", provider);
                    await DownloadBinaryAsync(provider, cancellationToken);
                    _logger.LogInformation("Successfully installed {Provider} CLI", provider);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download binary for {Provider}", provider);
                }
            }
        }
    }

    private async Task DownloadBinaryAsync(SpeedtestProvider provider, CancellationToken cancellationToken)
    {
        var url = GetDownloadUrl(provider);
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("No compatible binary URL found for {Provider} on this platform", provider);
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

            ExtractBinary(tempFile, provider);
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

    private static string? GetDownloadUrl(SpeedtestProvider provider)
    {
        var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win32" :
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "darwin" : "linux";

        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            Architecture.X86 => "ia32",
            _ => "x64"
        };

        return provider switch
        {
            SpeedtestProvider.Ookla => GetOoklaUrl(os, arch),
            SpeedtestProvider.Libre => GetLibreUrl(os, arch),
            SpeedtestProvider.Cloudflare => GetCloudflareUrl(os, arch),
            _ => null
        };
    }

    private static string? GetOoklaUrl(string os, string arch)
    {
        const string baseUrl = "https://install.speedtest.net/app/cli/ookla-speedtest-1.2.0-";
        if (os == "win32" && arch == "x64") return $"{baseUrl}win64.zip";
        if (os == "darwin" && arch == "x64") return $"{baseUrl}macosx-x86_64.tgz";
        if (os == "linux" && arch == "x64") return $"{baseUrl}linux-x86_64.tgz";
        if (os == "linux" && arch == "arm64") return $"{baseUrl}linux-aarch64.tgz";
        if (os == "linux" && arch == "arm") return $"{baseUrl}linux-armhf.tgz";
        if (os == "linux" && arch == "ia32") return $"{baseUrl}linux-i386.tgz";
        return null;
    }

    private static string? GetLibreUrl(string os, string arch)
    {
        const string baseUrl = "https://github.com/librespeed/speedtest-cli/releases/download/v1.0.10/librespeed-cli_1.0.10_";
        if (os == "win32" && arch == "x64") return $"{baseUrl}windows_amd64.zip";
        if (os == "win32" && arch == "arm64") return $"{baseUrl}windows_arm64.zip";
        if (os == "darwin" && arch == "x64") return $"{baseUrl}darwin_amd64.tar.gz";
        if (os == "darwin" && arch == "arm64") return $"{baseUrl}darwin_arm64.tar.gz";
        if (os == "linux" && arch == "x64") return $"{baseUrl}linux_amd64.tar.gz";
        if (os == "linux" && arch == "arm64") return $"{baseUrl}linux_arm64.tar.gz";
        if (os == "linux" && arch == "arm") return $"{baseUrl}linux_armv7.tar.gz";
        return null;
    }

    private static string? GetCloudflareUrl(string os, string arch)
    {
        const string baseUrl = "https://github.com/code-inflation/cfspeedtest/releases/download/v2.2.2/";
        if (os == "win32" && arch == "x64") return $"{baseUrl}cfspeedtest-x86_64-pc-windows-msvc.zip";
        if (os == "darwin" && arch == "x64") return $"{baseUrl}cfspeedtest-x86_64-apple-darwin.tar.gz";
        if (os == "darwin" && arch == "arm64") return $"{baseUrl}cfspeedtest-aarch64-apple-darwin.tar.gz";
        if (os == "linux" && arch == "x64") return $"{baseUrl}cfspeedtest-x86_64-unknown-linux-gnu.tar.gz";
        if (os == "linux" && arch == "arm64") return $"{baseUrl}cfspeedtest-aarch64-unknown-linux-gnu.tar.gz";
        return null;
    }

    private void ExtractBinary(string archivePath, SpeedtestProvider provider)
    {
        var targetPath = GetBinaryPath(provider);
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
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to set unix file mode on {Path}", path);
            }
        }
    }
}
