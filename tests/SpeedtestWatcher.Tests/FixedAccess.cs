using SpeedtestWatcher.Application.Security;

namespace SpeedtestWatcher.Tests;

internal sealed class FixedAccess(Access level) : ICurrentAccess
{
    public static FixedAccess Full { get; } = new(Access.Full);

    public static FixedAccess ReadOnly { get; } = new(Access.ReadOnly);

    public Access Level => level;
}
