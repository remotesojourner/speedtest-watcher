using System.Globalization;
using SpeedtestWatcher.Application.Providers;

namespace SpeedtestWatcher.Web.Helpers;

public static class ServerLabels
{
    public static IReadOnlyList<ServerInfo> NearestFirst(IEnumerable<ServerInfo> servers) =>
        servers.OrderBy(server => server.Distance ?? double.MaxValue).ToList();

    public static string For(ServerInfo server)
    {
        var location = string.Join(", ", new[] { server.Name, server.Country }.Where(part => !string.IsNullOrEmpty(part)));
        var head = !string.IsNullOrEmpty(server.Sponsor)
            ? location.Length == 0 ? server.Sponsor : $"{server.Sponsor} - {location}"
            : location.Length > 0 ? location : server.Host ?? server.Id;

        return server.Distance is { } distance ? $"{head} ({distance.ToString(CultureInfo.InvariantCulture)} km)" : head;
    }
}
