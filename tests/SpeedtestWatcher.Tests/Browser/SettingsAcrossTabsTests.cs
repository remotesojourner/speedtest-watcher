using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace SpeedtestWatcher.Tests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class SettingsAcrossTabsTests : BrowserTest, IClassFixture<BrowserAppWithResults>
{
    private readonly BrowserAppWithResults _app;

    public SettingsAcrossTabsTests(BrowserAppWithResults app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task ChangingTheProviderInOneTab_ChangesTheRunButtonInAnother()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var provider = await browser.NewPageAsync();
        var dashboard = await browser.NewPageAsync();
        await provider.GotoAsync("/settings/provider");
        await dashboard.GotoAsync("/");
        var runOnAServer = dashboard.GetByRole(AriaRole.Button, new() { Name = "Run on a specific server" });
        await Expect(runOnAServer).ToBeVisibleAsync();
        await Expect(provider.GetByText("Speedtest Provider")).ToBeVisibleAsync();

        await provider.Locator(".sw-option").Filter(new() { HasText = "Cloudflare" }).First.ClickAsync();
        await provider.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Expect(provider.GetByText("The provider settings have been saved")).ToBeVisibleAsync();

        await Expect(runOnAServer).ToBeHiddenAsync();
        AssertNoBrowserErrors();
    }
}
