using System.Reflection;

namespace SpeedtestWatcher.Web.Helpers;

public static class ProjectInfo
{
    private const string RepositorySetting = "Project:Repository";

    public static string Version { get; } =
        (typeof(ProjectInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0").Split('+')[0];

    public static string? Repository(IConfiguration configuration) =>
        configuration[RepositorySetting]?.Trim().Trim('/') is { Length: > 0 } repository ? repository : null;

    public static string? RepositoryUrl(IConfiguration configuration) =>
        Repository(configuration) is { } repository ? $"https://github.com/{repository}" : null;
}
