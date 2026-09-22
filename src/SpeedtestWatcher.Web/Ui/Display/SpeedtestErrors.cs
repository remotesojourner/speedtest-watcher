namespace SpeedtestWatcher.Web.Ui.Display;

public static class SpeedtestErrors
{
    private static readonly (string Match, string Message)[] _known =
    [
        ("Network unreachable", "Internet connection was unstable during the time of the test"),
        ("Timeout occurred in connect", "The test took too long and was canceled"),
        ("permission denied", "Speedtest Watcher has no permission to start this test"),
        ("Resource temporarily unavailable", "The test could not be performed because the resource was temporarily unavailable"),
        ("No route to host", "The test could not be performed because there was no route to the host"),
        ("Connection refused", "The test could not be performed because the connection was rejected"),
        ("timed out", "Internet connection was unstable during the time of the test"),
        ("Could not retrieve or read configuration", "Ookla couldn't download its test configuration from speedtest.net")
    ];

    public static string Describe(string? error)
    {
        if (string.IsNullOrWhiteSpace(error)) return "Unknown error";

        foreach (var (match, message) in _known)
        {
            if (error.Contains(match, StringComparison.Ordinal)) return message;
        }

        return error;
    }
}
