using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.SpeedTest;

namespace SpeedtestWatcher.Tests.Browser;

internal sealed class FixedServerList : IServerListProvider
{
    private static readonly IReadOnlyList<ServerInfo> Servers =
    [
        new("101", "London", "Test Fibre", "United Kingdom", 4.2, "london.test.example:8080"),
        new("102", "Leeds", "Test Fibre", "United Kingdom", 270.1, "leeds.test.example:8080"),
        new("103", "Paris", "Test Fibre", "France", 344.6, "paris.test.example:8080")
    ];

    public Task<IReadOnlyList<ServerInfo>?> GetServersAsync(SpeedtestProvider provider, CancellationToken cancellationToken = default) =>
        Task.FromResult(provider == SpeedtestProvider.Cloudflare ? null : Servers);
}
