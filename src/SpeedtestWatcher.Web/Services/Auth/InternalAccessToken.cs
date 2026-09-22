using System.Security.Cryptography;
using System.Text;

namespace SpeedtestWatcher.Web.Services.Auth;

public sealed class InternalAccessToken
{
    public const string HeaderName = "X-Speedtest-Watcher-Internal";

    public string Value { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public bool Matches(string? candidate) =>
        candidate != null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(candidate), Encoding.UTF8.GetBytes(Value));
}
