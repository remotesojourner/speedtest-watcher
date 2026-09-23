using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Web.Resources;

namespace SpeedtestWatcher.Web.Ui.Display;

public static class SpeedtestErrors
{
    private static (string Match, string Message)[] Known =>
    [
        ("Network unreachable", WebStrings.ErrorUnstableConnection),
        ("Timeout occurred in connect", WebStrings.ErrorTookTooLong),
        ("permission denied", WebStrings.Format(WebStrings.ErrorNoPermission, ProjectInfo.Name)),
        ("Resource temporarily unavailable", WebStrings.ErrorResourceUnavailable),
        ("No route to host", WebStrings.ErrorNoRoute),
        ("Connection refused", WebStrings.ErrorConnectionRefused),
        ("timed out", WebStrings.ErrorUnstableConnection),
        ("Could not retrieve or read configuration", WebStrings.ErrorOoklaConfiguration)
    ];

    public static string Describe(string? error)
    {
        if (string.IsNullOrWhiteSpace(error)) return WebStrings.UnknownError;

        foreach (var (match, message) in Known)
        {
            if (error.Contains(match, StringComparison.Ordinal)) return message;
        }

        return error;
    }
}
