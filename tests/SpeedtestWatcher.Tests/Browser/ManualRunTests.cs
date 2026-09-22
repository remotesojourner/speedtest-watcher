using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace SpeedtestWatcher.Tests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class ManualRunTests : BrowserTest, IClassFixture<BrowserAppWithResults>
{
    private readonly BrowserAppWithResults _app;

    public ManualRunTests(BrowserAppWithResults app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task AManualRun_ShowsAsRunningInAnotherTab_AndItsResultArrivesThereLive()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var dashboard = await browser.NewPageAsync();
        var history = await browser.NewPageAsync();
        await dashboard.GotoAsync("/");
        await history.GotoAsync("/history");
        await Expect(dashboard.GetByText("Overview")).ToBeVisibleAsync();
        await Expect(history.GetByText("Recent Tests")).ToBeVisibleAsync();
        var newestResult = history.Locator(".mud-list-item").First;
        await Expect(newestResult).Not.ToContainTextAsync(HeldSpeedtestRunner.DownloadShown);

        await dashboard.GetByRole(AriaRole.Button, new() { Name = "Run Test" }).ClickAsync();
        await Expect(dashboard.GetByText("Speedtest started")).ToBeVisibleAsync();
        await Expect(history.GetByRole(AriaRole.Button, new() { Name = "Running..." })).ToBeVisibleAsync();

        _app.Runner.Release();

        await Expect(history.GetByText("Speedtest complete")).ToBeVisibleAsync();
        await Expect(history.GetByRole(AriaRole.Button, new() { Name = "Run Test" })).ToBeVisibleAsync();
        await Expect(newestResult).ToContainTextAsync(HeldSpeedtestRunner.DownloadShown);
        AssertNoBrowserErrors();
    }
}
