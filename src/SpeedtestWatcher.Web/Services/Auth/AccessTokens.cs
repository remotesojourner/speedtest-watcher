using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace SpeedtestWatcher.Web.Services.Auth;

public sealed class InternalAccessToken
{
    public const string HeaderName = "X-Speedtest-Watcher-Internal";

    public string Value { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public bool Matches(string? candidate) => candidate != null && FixedTimeEquals(candidate, Value);

    internal static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}

public static class ApiToken
{
    public static string Generate() => "swt_" + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static bool Matches(string? candidate, string? storedHash) =>
        !string.IsNullOrEmpty(candidate)
        && !string.IsNullOrEmpty(storedHash)
        && InternalAccessToken.FixedTimeEquals(Hash(candidate), storedHash);

    public static string? FromRequest(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : null;
    }
}
