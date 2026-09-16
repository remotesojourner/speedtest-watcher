namespace SpeedtestWatcher.Core.Helpers;

public static class TemplateHelper
{
    public static string ReplaceVariables(string template, IDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        var now = DateTime.Now;
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["year"] = now.Year.ToString(),
            ["month"] = now.Month.ToString("D2"),
            ["day"] = now.Day.ToString("D2"),
            ["hour"] = now.Hour.ToString("D2"),
            ["minute"] = now.Minute.ToString("D2"),
            ["second"] = now.Second.ToString("D2")
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
