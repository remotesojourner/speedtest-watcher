using Microsoft.Extensions.Logging.Abstractions;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.UnitTests.Application.Providers;

public class ServerSelectorTests
{
    private static readonly string[] _allowList = ["1", "2"];

    private readonly ServerSelector _selector = new(new NearbyServers(), NullLogger<ServerSelector>.Instance);

    [Theory]
    [InlineData(ServerMode.Auto, null)]
    [InlineData(ServerMode.Pinned, "7")]
    public async Task AutomaticLeavesTheChoiceToTheProviderAndPinnedUsesTheSavedServer(ServerMode mode, string? expected)
    {
        Assert.Equal(expected, await _selector.SelectAsync(Settings(mode, ServerListMode.Allow, "7", ["1", "2"]), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AnAllowListIsChosenFrom()
    {
        var chosen = await _selector.SelectAsync(Settings(ServerMode.Random, ServerListMode.Allow, null, ["1", "2"]), TestContext.Current.CancellationToken);

        Assert.Contains(chosen, _allowList);
    }

    [Fact]
    public async Task ADenyListIsLeftOutOfTheNearbyServers()
    {
        var chosen = await _selector.SelectAsync(Settings(ServerMode.Random, ServerListMode.Deny, null, ["10", "11"]), TestContext.Current.CancellationToken);

        Assert.Equal("12", chosen);
    }

    [Fact]
    public async Task WithNothingToChooseFromTheProviderDecides()
    {
        Assert.Null(await _selector.SelectAsync(Settings(ServerMode.Random, ServerListMode.Allow, null, []), TestContext.Current.CancellationToken));
    }

    private static ProviderSettings Settings(ServerMode mode, ServerListMode listMode, string? pinnedId, IReadOnlyList<string> listed) =>
        new(SpeedtestProvider.Ookla, null, null, mode, listMode, new ServerChoice(pinnedId, listed), new ServerChoice(null, []));

    private sealed class NearbyServers : IServerListProvider
    {
        public Task<IReadOnlyList<ServerInfo>?> GetServersAsync(SpeedtestProvider provider, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ServerInfo>?>([new ServerInfo("10", "A"), new ServerInfo("11", "B"), new ServerInfo("12", "C")]);
    }
}
