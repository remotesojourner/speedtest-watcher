using System.Globalization;

namespace SpeedtestWatcher.Application.Integrations;

public static class TemplateHelper
{
    public static string ReplaceVariables(string template, IDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        var now = DateTime.Now;
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["year"] = now.Year.ToString(CultureInfo.InvariantCulture),
            ["month"] = now.Month.ToString("D2", CultureInfo.InvariantCulture),
            ["day"] = now.Day.ToString("D2", CultureInfo.InvariantCulture),
            ["hour"] = now.Hour.ToString("D2", CultureInfo.InvariantCulture),
            ["minute"] = now.Minute.ToString("D2", CultureInfo.InvariantCulture),
            ["second"] = now.Second.ToString("D2", CultureInfo.InvariantCulture)
        };

        foreach (var (k, v) in variables)
        {
            dict[k] = v;
        }

        var result = template;
        foreach (var (k, v) in dict)
        {
            result = result.Replace($"%{k}%", v);
        }

        return result;
    }
}
