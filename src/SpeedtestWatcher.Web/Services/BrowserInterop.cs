using Microsoft.JSInterop;

namespace SpeedtestWatcher.Web.Services;

public sealed class BrowserInterop
{
    private readonly IJSRuntime _js;
    private readonly ILogger<BrowserInterop> _logger;

    public BrowserInterop(IJSRuntime js, ILogger<BrowserInterop> logger)
    {
        _js = js;
        _logger = logger;
    }

    public async Task<string?> GetLocalStorageAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<string?>("speedtestWatcherInterop.getLocalStorage", key);
        }
        catch (Exception ex) when (IsBrowserFailure(ex))
        {
            LogFailure(ex, $"Could not read '{key}' from the browser's storage");
            return null;
        }
    }

    public async Task SetLocalStorageAsync(string key, string value)
    {
        try
        {
            await _js.InvokeVoidAsync("speedtestWatcherInterop.setLocalStorage", key, value);
        }
        catch (Exception ex) when (IsBrowserFailure(ex))
        {
            LogFailure(ex, $"Could not save '{key}' to the browser's storage");
        }
    }

    public async Task<bool> CopyTextAsync(string text)
    {
        try
        {
            return await _js.InvokeAsync<bool>("speedtestWatcherInterop.copyText", text);
        }
        catch (Exception ex) when (IsBrowserFailure(ex))
        {
            LogFailure(ex, "Could not copy text to the clipboard");
            return false;
        }
    }

    public async Task<bool> DownloadAsync(string fileName, byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content);
            using var reference = new DotNetStreamReference(stream);
            await _js.InvokeVoidAsync("speedtestWatcherInterop.downloadFileFromStream", fileName, reference);
            return true;
        }
        catch (Exception ex) when (IsBrowserFailure(ex))
        {
            LogFailure(ex, $"Could not download {fileName}");
            return false;
        }
    }

    private static bool IsBrowserFailure(Exception ex) => ex is JSException or JSDisconnectedException or TaskCanceledException;

    private void LogFailure(Exception ex, string message)
    {
        var browserWentAway = ex is JSDisconnectedException or TaskCanceledException;
        _logger.Log(browserWentAway ? LogLevel.Debug : LogLevel.Warning, ex, "{Message}", message);
    }
}
