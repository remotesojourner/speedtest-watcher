namespace SpeedtestWatcher.Web.Services.Auth;

public static class BearerToken
{
    public static string? FromRequest(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : null;
    }
}
