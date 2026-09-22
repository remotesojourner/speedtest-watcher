using System.Globalization;

namespace SpeedtestWatcher.Web.Ui.Display;

public static class SettingNumber
{
    public static double Parse(string? raw) =>
        double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;

    public static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
