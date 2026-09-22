namespace SpeedtestWatcher.Application.Common;

public static class WebAddress
{
    public static bool IsHttp(string? value) =>
        Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
