using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace SpeedtestWatcher.Tests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class PauseAcrossTabsTests : BrowserTest, IClassFixture<BrowserAppWithResults>
{
    private readonly BrowserAppWithResults _app;

    public PauseAcrossTabsTests(BrowserAppWithResults app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task PausingInOneTab_ShowsInAnother_AndResumingThereClearsBoth()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var schedule = await browser.NewPageAsync();
        var dashboard = await browser.NewPageAsync();
        await schedule.GotoAsync("/settings/schedule");
        await dashboard.GotoAsync("/");
        var pauseButton = schedule.GetByRole(AriaRole.Button, new() { Name = "Pause Tests" });
        await Expect(pauseButton).ToBeVisibleAsync();
        await Expect(dashboard.GetByText("Overview")).ToBeVisibleAsync();

        await pauseButton.ClickAsync();
        await Expect(dashboard.GetByText("Paused", new() { Exact = true })).ToBeVisibleAsync();

        await dashboard.GetByRole(AriaRole.Button, new() { Name = "Resume", Exact = true }).ClickAsync();
        await Expect(pauseButton).ToBeVisibleAsync();
        await Expect(dashboard.GetByText("Paused", new() { Exact = true })).ToBeHiddenAsync();
        await Expect(schedule.GetByText("Paused", new() { Exact = true })).ToBeHiddenAsync();
        AssertNoBrowserErrors();
    }
}
