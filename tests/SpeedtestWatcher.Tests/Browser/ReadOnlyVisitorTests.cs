using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace SpeedtestWatcher.Tests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class ReadOnlyVisitorTests : BrowserTest, IClassFixture<BrowserAppForReadOnlyVisitors>
{
    private readonly BrowserAppForReadOnlyVisitors _app;

    public ReadOnlyVisitorTests(BrowserAppForReadOnlyVisitors app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task TheDashboard_OffersSignIn_InsteadOfTheRunButton()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/");
        await Expect(page.GetByText("Overview")).ToBeVisibleAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Sign in" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Run Test" })).ToHaveCountAsync(0);
        AssertNoBrowserErrors();
    }

    [Fact]
    public async Task AResultsDetails_HaveNoDeleteButton()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/history");
        await page.Locator(".mud-list-item").First.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Close" })).ToBeVisibleAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Delete" })).ToHaveCountAsync(0);
        AssertNoBrowserErrors();
    }

    [Fact]
    public async Task Settings_ShowOnlyTheReadOnlyView()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/settings");
        await Expect(page.GetByText("This instance is read-only")).ToBeVisibleAsync();

        await Expect(page.GetByText("Display Preferences", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText("API Token")).ToHaveCountAsync(0);
        AssertNoBrowserErrors();
    }
}
