using System.Globalization;
using System.Text.RegularExpressions;

namespace SpeedtestWatcher.Web.Ui.Display;

public static partial class FormatSegments
{
    public static IReadOnlyList<(string Text, int? Argument)> Split(string format) =>
    [
        .. Placeholder().Split(format).Select((part, index) => index % 2 == 0
            ? (part, (int?)null)
            : ("", int.Parse(part, CultureInfo.InvariantCulture)))
    ];

    [GeneratedRegex(@"\{(\d+)\}")]
    private static partial Regex Placeholder();
}
