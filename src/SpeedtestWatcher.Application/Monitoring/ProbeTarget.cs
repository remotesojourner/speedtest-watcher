using System.Globalization;
using System.Net;

namespace SpeedtestWatcher.Application.Monitoring;

public sealed record ProbeTarget(string Host, int Port)
{
    public static ProbeTarget? Parse(string value)
    {
        var text = value.Trim();
        if (text.Length == 0) return null;

        var separator = text.LastIndexOf(':');
        if (separator <= 0 || separator == text.Length - 1) return null;

        var host = text[..separator].Trim().Trim('[', ']');
        if (host.Length == 0 || host.Contains(' ', StringComparison.Ordinal)) return null;
        if (host.Contains(':', StringComparison.Ordinal) && !IPAddress.TryParse(host, out _)) return null;

        return int.TryParse(text[(separator + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) && port is > 0 and <= 65535
            ? new ProbeTarget(host, port)
            : null;
    }

    public static IReadOnlyList<ProbeTarget> ParseList(IEnumerable<string> values) =>
        [.. values.Select(Parse).OfType<ProbeTarget>()];

    public override string ToString() =>
        Host.Contains(':', StringComparison.Ordinal) ? $"[{Host}]:{Port}" : $"{Host}:{Port}";
}
