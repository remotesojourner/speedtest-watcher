using static Microsoft.Playwright.Assertions;

namespace SpeedtestWatcher.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class PageLayoutTests : BrowserTest, IClassFixture<BrowserAppWithResults>
{
    private static readonly (string Path, string Name, string Landmark)[] Pages =
    [
        ("/", "dashboard", "Overview"),
        ("/history", "history", "Recent Tests"),
        ("/settings/general", "general", "Before Each Test"),
        ("/settings/schedule", "schedule", "Pause Speedtests"),
        ("/settings/provider", "provider", "Speedtest Provider"),
        ("/settings/display", "display", "Display Preferences"),
        ("/settings/integrations", "integrations", "Add integration"),
        ("/settings/security", "security", "API Token"),
        ("/settings/storage", "storage", "Data Retention"),
        ("/settings/about", "about", "Links"),
        ("/welcome", "welcome", "Welcome to Speedtest Watcher!")
    ];

    private readonly BrowserAppWithResults _app;

    public PageLayoutTests(BrowserAppWithResults app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    public static TheoryData<BrowserTheme, int> Views => new()
    {
        { BrowserTheme.Dark, DesktopWidth },
        { BrowserTheme.Light, DesktopWidth },
        { BrowserTheme.Dark, PhoneWidth },
        { BrowserTheme.Light, PhoneWidth }
    };

    [Theory]
    [MemberData(nameof(Views))]
    public async Task EveryPage_RendersWhole_WithoutScrollingSideways(BrowserTheme theme, int width)
    {
        await using var browser = await OpenBrowserAsync(_app, theme, width);
        var page = await browser.NewPageAsync();
        Directory.CreateDirectory(ScreenshotFolder);
        var problems = new List<string>();

        foreach (var (path, name, landmark) in Pages)
        {
            await page.GotoAsync(path);
            await Expect(page.GetByText(landmark).First).ToBeVisibleAsync();
            await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotFolder, $"{name}-{theme.ToString().ToLowerInvariant()}-{width}.png"), FullPage = name != "dashboard" });

            if (await ShowsAnErrorAsync(page)) problems.Add($"{path} shows an error");
            if (await ScrollsSidewaysAsync(page)) problems.Add($"{path} scrolls sideways");
        }

        Assert.Empty(problems);
        AssertNoBrowserErrors();
    }
}
