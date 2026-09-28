using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using SpeedtestWatcher.Application.Enums;

namespace SpeedtestWatcher.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class Iperf3ProviderTests : BrowserTest, IClassFixture<BrowserAppWithResults>
{
    private readonly BrowserAppWithResults _app;

    public Iperf3ProviderTests(BrowserAppWithResults app, Chromium chromium) : base(chromium)
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

    [Fact]
    public async Task Iperf3ServersAreAddedCheckedAndSaved()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();
        await page.GotoAsync("/settings/provider");
        await ChooseIperf3Async(page);
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true });
        var add = page.GetByLabel("Add a server");

        await Expect(page.GetByText("No servers added yet. Add at least one to run a test.")).ToBeVisibleAsync();
        await Expect(save).ToBeDisabledAsync();

        await add.FillAsync("not a server");
        await Expect(page.GetByText("Enter a host and port, like 192.168.1.10:5201")).ToBeVisibleAsync();
        await Expect(save).ToBeDisabledAsync();

        await add.FillAsync("192.168.1.10:5201");
        await add.PressAsync("Enter");
        await Expect(page.GetByText("192.168.1.10:5201", new() { Exact = true })).ToBeVisibleAsync();
        await add.FillAsync("[2001:db8::5]:5202");
        await save.ClickAsync();
        await Expect(page.GetByText("The provider settings have been saved")).ToBeVisibleAsync();

        var saved = (await _app.SettingsAsync(TestContext.Current.CancellationToken)).Provider;
        Assert.Equal(SpeedtestProvider.Iperf3, saved.Selected);
        Assert.Equal(["192.168.1.10:5201", "[2001:db8::5]:5202"], saved.Iperf3Servers.Select(server => server.ToString()));

        await page.ReloadAsync();
        await Expect(page.GetByText("[2001:db8::5]:5202", new() { Exact = true })).ToBeVisibleAsync();
        AssertNoBrowserErrors();
    }

    [Theory]
    [MemberData(nameof(Views))]
    public async Task TheIperf3SectionFitsEveryView(BrowserTheme theme, int width)
    {
        await using var browser = await OpenBrowserAsync(_app, theme, width);
        var page = await browser.NewPageAsync();
        await page.GotoAsync("/settings/provider");
        await ChooseIperf3Async(page);
        var add = page.GetByLabel("Add a server");
        await add.FillAsync("speedtest.lan:5201");
        await add.PressAsync("Enter");
        await Expect(page.GetByText("speedtest.lan:5201", new() { Exact = true })).ToBeVisibleAsync();

        Directory.CreateDirectory(ScreenshotFolder);
        await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotFolder, $"provider-iperf3-{theme.ToString().ToLowerInvariant()}-{width}.png"), FullPage = true });

        Assert.False(await ScrollsSidewaysAsync(page));
        AssertNoBrowserErrors();
    }

    private static async Task ChooseIperf3Async(IPage page)
    {
        await page.Locator(".sw-option").Filter(new() { HasText = "Test against a server you run yourself" }).ClickAsync();
        await Expect(page.GetByText("Your iperf3 servers")).ToBeVisibleAsync();
    }
}
