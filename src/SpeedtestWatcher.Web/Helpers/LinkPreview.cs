using System.Globalization;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Web.Helpers;

public sealed record LinkPreview(string Subtitle, string Ping, string? PingCaption, string Download, string Upload)
{
    private const string NoValue = "--";

    public static LinkPreview For(Speedtest? test) => test == null
        ? new LinkPreview("No completed speedtests yet", NoValue, null, NoValue, NoValue)
        : new LinkPreview(
            Invariant($"Latest test: {test.Created:yyyy-MM-dd HH:mm:ss} UTC"),
            Invariant($"{test.Ping} ms"),
            test.Jitter is { } jitter ? Invariant($"±{jitter:F1} ms jitter") : null,
            Invariant($"{test.Download:F1}"),
            Invariant($"{test.Upload:F1}"));

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
