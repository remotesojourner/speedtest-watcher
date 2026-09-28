using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models;

namespace SpeedtestWatcher.Application.Configuration;

public sealed record ProviderSettings(
    SpeedtestProvider Selected,
    string? Interface,
    string? LibreUrl,
    ServerMode ServerMode,
    ServerListMode ServerListMode,
    ServerChoice Ookla,
    ServerChoice Libre,
    IReadOnlyList<ProbeTarget> Iperf3Servers)
{
    public ServerChoice? ServersFor(SpeedtestProvider provider) => provider switch
    {
        SpeedtestProvider.Ookla => Ookla,
        SpeedtestProvider.Libre => Libre,
        _ => null
    };
}
