using SpeedtestWatcher.Core.Enums;

namespace SpeedtestWatcher.Core.Settings;

public sealed record ProviderSettings(
    SpeedtestProvider Selected,
    string? Interface,
    string? LibreUrl,
    ServerMode ServerMode,
    ServerListMode ServerListMode,
    ServerChoice Ookla,
    ServerChoice Libre)
{
    public ServerChoice? ServersFor(SpeedtestProvider provider) => provider switch
    {
        SpeedtestProvider.Ookla => Ookla,
        SpeedtestProvider.Libre => Libre,
        _ => null
    };
}
