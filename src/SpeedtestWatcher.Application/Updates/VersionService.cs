using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Updates;

public sealed class VersionService
{
    private readonly IReleaseChecker _releases;

    public VersionService(IReleaseChecker releases)
    {
        _releases = releases;
    }

    public async Task<VersionInfoDto> GetVersionAsync(CancellationToken cancellationToken = default) => new()
    {
        Local = ProjectInfo.Version,
        Remote = await _releases.GetLatestVersionAsync(cancellationToken) ?? "0"
    };
}
