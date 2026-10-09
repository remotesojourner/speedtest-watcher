using static Microsoft.Playwright.Assertions;

namespace SpeedtestWatcher.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class TargetColourTests : BrowserTest, IClassFixture<BrowserAppWithResults>
{
    private const string ResolvePaletteColour = """
        name => {
            const probe = document.createElement('span');
            probe.style.color = `var(--mud-palette-${name})`;
            document.body.append(probe);
            const colour = getComputedStyle(probe).color;
            probe.remove();
            return colour;
        }
        """;

    private readonly BrowserAppWithResults _app;

    public TargetColourTests(BrowserAppWithResults app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task HistoryColoursResultsByTheTargetsTheyWereJudgedAgainst()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var raised = await _app.SaveSettingsAsync(new Dictionary<string, string> { ["ping"] = "5", ["download"] = "2000", ["upload"] = "500" }, cancellationToken);
        Assert.True(raised.Succeeded, raised.Error);
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();
        await page.GotoAsync("/history");
        var oldestDownload = page.GetByText("930.0 Mbps");
        await Expect(oldestDownload).ToBeVisibleAsync();
        var success = await page.EvaluateAsync<string>(ResolvePaletteColour, "success");

        await Expect(page.GetByText("902.0", new() { Exact = true })).ToHaveCSSAsync("color", success);
        await Expect(oldestDownload.Locator("xpath=..").Locator("svg")).ToHaveCSSAsync("color", success);
        AssertNoBrowserErrors();
    }
}
