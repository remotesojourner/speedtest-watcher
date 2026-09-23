using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SpeedtestWatcher.UnitTests;

public partial class ResourceStringsTests
{
    private static readonly (string Project, string ClassName)[] _resourceFiles =
    [
        ("SpeedtestWatcher.Application", "ApplicationStrings"),
        ("SpeedtestWatcher.Web", "WebStrings")
    ];

    public static TheoryData<string, string> ResourceFiles() => [.. _resourceFiles];

    [Theory]
    [MemberData(nameof(ResourceFiles))]
    public void EveryStringIsUsed(string project, string className)
    {
        var used = Usage()
            .Matches(ProjectSource(project))
            .Where(match => match.Groups["class"].Value == className)
            .Select(match => match.Groups["key"].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Empty(Strings(NeutralFile(project, className)).Keys.Except(used, StringComparer.Ordinal));
    }

    [Fact]
    public void TranslationsKeepTheEnglishKeysAndPlaceholders()
    {
        List<string> problems = [];
        foreach (var (project, className) in _resourceFiles)
        {
            var english = Strings(NeutralFile(project, className));
            problems.AddRange(english.Where(entry => !IsValidFormat(entry.Value)).Select(entry => $"{className}.resx: {entry.Key} isn't a valid format string"));

            foreach (var translation in Directory.GetFiles(Path.GetDirectoryName(NeutralFile(project, className))!, $"{className}.*.resx"))
            {
                var file = Path.GetFileName(translation);
                foreach (var (key, text) in Strings(translation))
                {
                    if (!english.TryGetValue(key, out var original)) problems.Add($"{file}: {key} isn't in the English file");
                    else if (!IsValidFormat(text)) problems.Add($"{file}: {key} isn't a valid format string");
                    else if (!Placeholders(text).SequenceEqual(Placeholders(original))) problems.Add($"{file}: {key} doesn't have the placeholders of the English text");
                }
            }
        }

        Assert.Empty(problems);
    }

    private static Dictionary<string, string> Strings(string resxFile) =>
        XDocument.Load(resxFile).Root!.Elements("data")
            .ToDictionary(data => (string)data.Attribute("name")!, data => (string?)data.Element("value") ?? "", StringComparer.Ordinal);

    private static bool IsValidFormat(string text)
    {
        try
        {
            CompositeFormat.Parse(text);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static IEnumerable<int> Placeholders(string text) =>
        Placeholder().Matches(text).Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)).Distinct().Order();

    private static string ProjectSource(string project) =>
        string.Join('\n', Directory.EnumerateFiles(Path.Combine(SourceRoot(), project), "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".cs" or ".razor")
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Intersect(["bin", "obj"]).Any())
            .Select(File.ReadAllText));

    private static string NeutralFile(string project, string className) =>
        Path.Combine(SourceRoot(), project, "Resources", $"{className}.resx");

    private static string SourceRoot([CallerFilePath] string testFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile)!, "..", "..", "src"));

    [GeneratedRegex(@"\b(?<class>ApplicationStrings|WebStrings)\.(?<key>[A-Z][A-Za-z0-9]*)")]
    private static partial Regex Usage();

    [GeneratedRegex(@"\{(\d+)[^}]*\}")]
    private static partial Regex Placeholder();
}
