using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Web.Helpers;

public static class UpdateCheck
{
    public static string? AvailableUpdate(VersionInfoDto? versions) =>
        versions != null
        && Version.TryParse(versions.Remote, out var remote)
        && Version.TryParse(versions.Local, out var local)
        && WithAllParts(remote) > WithAllParts(local)
            ? versions.Remote
            : null;

    private static Version WithAllParts(Version version) =>
        new(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));
}
