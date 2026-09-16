using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace SpeedtestWatcher.Web.Services.Auth;

/// <summary>
/// A secret that exists only inside this process. The UI calls the app's own API over loopback, where the
/// browser's sign-in cookie isn't available, so a signed-in session sends this instead.
/// </summary>
public sealed class InternalAccessToken
{
    public const string HeaderName = "X-Speedtest-Watcher-Internal";

    public string Value { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public bool Matches(string? candidate) => candidate != null && FixedTimeEquals(candidate, Value);

    internal static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}

/// <summary>The token Prometheus and scripts send as a bearer token. Only its hash is stored.</summary>
public static class ApiToken
{
    public static string Generate() => "swt_" + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static bool Matches(string? candidate, string? storedHash) =>
        !string.IsNullOrEmpty(candidate)
        && !string.IsNullOrEmpty(storedHash)
        && InternalAccessToken.FixedTimeEquals(Hash(candidate), storedHash);

    /// <summary>The token from an <c>Authorization: Bearer ...</c> header, if there is one.</summary>
    public static string? FromRequest(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : null;
    }
}
