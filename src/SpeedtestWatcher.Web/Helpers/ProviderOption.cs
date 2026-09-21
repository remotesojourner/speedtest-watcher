using SpeedtestWatcher.Core.Enums;

namespace SpeedtestWatcher.Web.Helpers;

public sealed record ProviderOption(SpeedtestProvider Provider, string Title, string Description, string Image)
{
    public static IReadOnlyList<ProviderOption> All { get; } =
    [
        new(SpeedtestProvider.Ookla, "Ookla", "Popular provider with a global server network", "img/ookla.webp"),
        new(SpeedtestProvider.Libre, "LibreSpeed", "Open-source, self-hostable speedtest", "img/libre.webp"),
        new(SpeedtestProvider.Cloudflare, "Cloudflare", "Fast CDN-based testing", "img/cloudflare.webp")
    ];
}
