using SpeedtestWatcher.Web.Ui.Display;

namespace SpeedtestWatcher.UnitTests.Web.Ui;

public class FormatSegmentsTests
{
    [Fact]
    public void SplitSeparatesTheTextFromTheNumberedPlaceholders()
    {
        (string Text, int? Argument)[] expected = [("I accept ", null), ("", 0), (", ", null), ("", 1), (" and ", null), ("", 2), (".", null)];

        Assert.Equal(expected, FormatSegments.Split("I accept {0}, {1} and {2}."));
    }

    [Fact]
    public void SplitKeepsTextWithoutPlaceholdersWhole()
    {
        (string Text, int? Argument)[] expected = [("Nothing to fill in", null)];

        Assert.Equal(expected, FormatSegments.Split("Nothing to fill in"));
    }
}
