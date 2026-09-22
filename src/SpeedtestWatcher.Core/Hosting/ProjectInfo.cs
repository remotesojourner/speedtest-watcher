using System.Reflection;

namespace SpeedtestWatcher.Core.Hosting;

public static class ProjectInfo
{
    private const string Owner = "remotesojourner";

    public const string Repository = $"{Owner}/speedtest-watcher";

    public const string RepositoryUrl = $"https://github.com/{Repository}";

    public const string SponsorUrl = $"https://github.com/sponsors/{Owner}";

    public static string Version { get; } =
        (typeof(ProjectInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0").Split('+')[0];
}
