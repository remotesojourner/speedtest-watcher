namespace SpeedtestWatcher.Web.Ui.Display;

public static class IntegrationIcons
{
    public const string Folder = "img/integrations";

    private static readonly Dictionary<string, string> Files = new()
    {
        ["discord"] = "discord.svg",
        ["telegram"] = "telegram.svg",
        ["gotify"] = "gotify.svg",
        ["ntfy"] = "ntfy.svg",
        ["pushover"] = "pushover.svg",
        ["apprise"] = "apprise.webp",
        ["webhook"] = "webhook.svg",
        ["healthChecks"] = "healthchecks.svg",
        ["influxdb"] = "influxdb.svg"
    };

    public static IReadOnlyCollection<string> Integrations => Files.Keys;

    public static string For(string integration) => $"{Folder}/{Files[integration]}";
}
