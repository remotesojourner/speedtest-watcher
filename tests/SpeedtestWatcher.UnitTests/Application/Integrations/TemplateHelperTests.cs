using SpeedtestWatcher.Application.Integrations;

namespace SpeedtestWatcher.UnitTests.Application.Integrations;

public class TemplateHelperTests
{
    [Fact]
    public void ReplaceVariables_FillsEveryPlaceholder()
    {
        var vars = new Dictionary<string, string>
        {
            ["ping"] = "15",
            ["download"] = "250.50",
            ["upload"] = "50.25",
            ["error"] = "Connection refused"
        };

        var template = "Ping: %ping% ms, Down: %download% Mbps, Up: %upload% Mbps, Error: %error%";
        var result = TemplateHelper.ReplaceVariables(template, vars);

        Assert.Equal("Ping: 15 ms, Down: 250.50 Mbps, Up: 50.25 Mbps, Error: Connection refused", result);
    }
}
