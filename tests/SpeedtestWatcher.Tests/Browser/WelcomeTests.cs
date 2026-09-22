using Microsoft.Playwright;
using SpeedtestWatcher.Core.Enums;
using static Microsoft.Playwright.Assertions;

namespace SpeedtestWatcher.Tests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class WelcomeTests : BrowserTest, IClassFixture<BrowserAppWithoutProvider>
{
    private readonly BrowserAppWithoutProvider _app;

    public WelcomeTests(BrowserAppWithoutProvider app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task TheWelcomeSteps_SaveTheProviderAndTheTargets()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/");
        await Expect(page.GetByText("Welcome to Speedtest Watcher!")).ToBeVisibleAsync();
        await ContinueAsync(page);
        await Expect(page.GetByText("Choose a provider")).ToBeVisibleAsync();
        await ContinueAsync(page);
        await Expect(page.GetByText("Set your optimal values")).ToBeVisibleAsync();
        await page.GetByLabel("Download (Mbps)").FillAsync("500");
        await ContinueAsync(page);
        await Expect(page.GetByText("Accept Ookla's terms")).ToBeVisibleAsync();
        await page.GetByText("I have read and accept these terms").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Done" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Dashboard", Exact = true })).ToBeVisibleAsync();

        var settings = await _app.SettingsAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SpeedtestProvider.Ookla, settings.Provider.Selected);
        Assert.Equal(500, settings.Targets.Download);
        AssertNoBrowserErrors();
    }

    private static Task ContinueAsync(IPage page) => page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();
}
