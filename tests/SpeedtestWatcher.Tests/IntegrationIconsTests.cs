using System.Runtime.CompilerServices;
using SpeedtestWatcher.Web.Ui.Display;

namespace SpeedtestWatcher.Tests;

public class IntegrationIconsTests
{
    [Fact]
    public void EveryIntegrationType_HasAnIcon_AndEveryIconFileIsUsed()
    {
        var types = TestIntegrations.Dispatcher(new InMemoryIntegrations([]), new RecordingHandler()).Schemas.Keys;
        var webRoot = WebRoot();

        Assert.Equal(types.Order(StringComparer.Ordinal), IntegrationIcons.Integrations.Order(StringComparer.Ordinal));

        var expectedFiles = types.Select(type => Path.GetFullPath(Path.Combine(webRoot, IntegrationIcons.For(type)))).Order(StringComparer.Ordinal);
        var actualFiles = Directory.GetFiles(Path.Combine(webRoot, IntegrationIcons.Folder)).Select(Path.GetFullPath).Order(StringComparer.Ordinal);
        Assert.Equal(expectedFiles, actualFiles);
    }

    private static string WebRoot([CallerFilePath] string testFile = "") =>
        Path.Combine(Path.GetDirectoryName(testFile)!, "..", "..", "src", "SpeedtestWatcher.Web", "wwwroot");
}
