using System.Globalization;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Web.Resources;

namespace SpeedtestWatcher.Web.Api;

public sealed record LinkPreview(string Subtitle, string Ping, string? PingCaption, string Download, string Upload)
{
    private const string NoValue = "--";

    public static LinkPreview For(Speedtest? test) => test == null
        ? new LinkPreview(WebStrings.LinkPreviewNoTests, NoValue, null, NoValue, NoValue)
        : new LinkPreview(
            WebStrings.Format(WebStrings.LinkPreviewLatestTest, Invariant($"{test.Created:yyyy-MM-dd HH:mm:ss}")),
            Invariant($"{test.Ping} ms"),
            test.Jitter is { } jitter ? WebStrings.Format(WebStrings.LinkPreviewJitter, Invariant($"{jitter:F1}")) : null,
            Invariant($"{test.Download:F1}"),
            Invariant($"{test.Upload:F1}"));

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
