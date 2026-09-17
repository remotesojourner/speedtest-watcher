using System.Diagnostics.CodeAnalysis;

namespace SpeedtestWatcher.Web.Helpers;

public sealed record ProviderOption(string Id, string Title, string Description, string Image)
{
    public static IReadOnlyList<ProviderOption> All { get; } =
    [
        new("ookla", "Ookla", "Popular provider with a global server network", "img/ookla.webp"),
        new("libre", "LibreSpeed", "Open-source, self-hostable speedtest", "img/libre.webp"),
        new("cloudflare", "Cloudflare", "Fast CDN-based testing", "img/cloudflare.webp")
    ];

    public static bool IsKnown([NotNullWhen(true)] string? id) => All.Any(option => option.Id == id);
}
