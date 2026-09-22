using System.Collections.Concurrent;
using Microsoft.Playwright;

namespace SpeedtestWatcher.IntegrationTests.Browser;

public abstract class BrowserTest
{
    public const int DesktopWidth = 1280;
    public const int PhoneWidth = 390;

    private readonly Chromium _chromium;
    private readonly ConcurrentQueue<string> _browserErrors = new();

    static BrowserTest()
    {
        Assertions.SetDefaultExpectTimeout(15000);
    }

    protected BrowserTest(Chromium chromium)
    {
        _chromium = chromium;
    }

    protected static string ScreenshotFolder { get; } = Path.Combine(AppContext.BaseDirectory, "TestResults", "browser");

    protected async Task<IBrowserContext> OpenBrowserAsync(BrowserApp app, BrowserTheme theme = BrowserTheme.Dark, int width = DesktopWidth)
    {
        if (_chromium.Browser is not { } browser)
        {
            Assert.Skip(_chromium.Unavailable);
            throw new InvalidOperationException(_chromium.Unavailable);
        }

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = app.BaseAddress.ToString(),
            ViewportSize = new ViewportSize { Width = width, Height = width == PhoneWidth ? 844 : 900 },
            TimezoneId = "America/New_York",
            Locale = "en-US"
        });
        context.SetDefaultTimeout(15000);
        await context.AddInitScriptAsync($"if (location.origin !== 'null') localStorage.setItem('theme', '{theme.ToString().ToLowerInvariant()}')");
        context.Page += (_, page) => Watch(page);
        return context;
    }

    protected void AssertNoBrowserErrors() => Assert.True(_browserErrors.IsEmpty, string.Join(Environment.NewLine, _browserErrors));

    protected static async Task<bool> ShowsAnErrorAsync(IPage page) =>
        await page.GetByText("Something went wrong").CountAsync() > 0 || await page.Locator("#blazor-error-ui").IsVisibleAsync();

    protected static Task<bool> ScrollsSidewaysAsync(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth");

    private void Watch(IPage page)
    {
        page.Console += (_, message) =>
        {
            if (message.Type == "error") _browserErrors.Enqueue($"{page.Url}: {message.Text}");
        };
        page.PageError += (_, error) => _browserErrors.Enqueue($"{page.Url}: {error}");
    }
}
