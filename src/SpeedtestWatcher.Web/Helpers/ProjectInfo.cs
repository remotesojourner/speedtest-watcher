using System.Reflection;

namespace SpeedtestWatcher.Web.Helpers;

/// <summary>The app's version and, once configured, its GitHub repository.</summary>
public static class ProjectInfo
{
    private const string RepositorySetting = "Project:Repository";

    /// <summary>The version from the project file, without the "+commit" suffix SourceLink appends.</summary>
    public static string Version { get; } =
        (typeof(ProjectInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0").Split('+')[0];

    /// <summary>The "owner/repo" from appsettings.json, or null while none is configured.</summary>
    public static string? Repository(IConfiguration configuration) =>
        configuration[RepositorySetting]?.Trim().Trim('/') is { Length: > 0 } repository ? repository : null;

    public static string? RepositoryUrl(IConfiguration configuration) =>
        Repository(configuration) is { } repository ? $"https://github.com/{repository}" : null;
}
