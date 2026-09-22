using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace SpeedtestWatcher.Application.SignIn;

public static class ApiToken
{
    public static string Generate() => "swt_" + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static bool Matches(string? candidate, string? storedHash) =>
        !string.IsNullOrEmpty(candidate)
        && !string.IsNullOrEmpty(storedHash)
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Hash(candidate)), Encoding.UTF8.GetBytes(storedHash));
}
