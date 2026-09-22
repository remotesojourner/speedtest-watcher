using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.TestSupport;

internal sealed class FixedAccess(Access level) : ICurrentAccess
{
    public static FixedAccess Full { get; } = new(Access.Full);

    public static FixedAccess ReadOnly { get; } = new(Access.ReadOnly);

    public Access Level => level;
}
