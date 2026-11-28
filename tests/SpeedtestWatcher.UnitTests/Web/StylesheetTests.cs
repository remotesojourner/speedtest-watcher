using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SpeedtestWatcher.UnitTests.Web;

public partial class StylesheetTests
{
    [Fact]
    public void EveryVariableReferenceNamesACustomProperty()
    {
        var invalidReferences = Directory.GetFiles(StylesheetFolder(), "*.css")
            .SelectMany(file => File.ReadLines(file).Select((text, index) => (File: Path.GetFileName(file), Line: index + 1, Text: text)))
            .Where(line => VariableWithoutCustomPropertyName().IsMatch(line.Text))
            .Select(line => $"{line.File}:{line.Line} {line.Text.Trim()}");

        Assert.Empty(invalidReferences);
    }

    [GeneratedRegex(@"var\(\s*(?!--)")]
    private static partial Regex VariableWithoutCustomPropertyName();

    private static string StylesheetFolder([CallerFilePath] string testFile = "") =>
        Path.Combine(Path.GetDirectoryName(testFile)!, "..", "..", "..", "src", "SpeedtestWatcher.Web", "wwwroot", "css");
}
