using SpeedtestWatcher.Application.Models.Dtos;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Application.Utils;

namespace SpeedtestWatcher.Application.Services;

public sealed class VersionService
{
    private readonly IReleaseService _releases;

    public VersionService(IReleaseService releases)
    {
        _releases = releases;
    }

    public async Task<VersionInfoDto> GetVersionAsync(CancellationToken cancellationToken = default) => new()
    {
        Local = ProjectInfo.Version,
        Remote = await _releases.GetLatestVersionAsync(cancellationToken) ?? "0"
    };
}
