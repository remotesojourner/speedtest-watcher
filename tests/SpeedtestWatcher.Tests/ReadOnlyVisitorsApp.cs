namespace SpeedtestWatcher.Tests;

public sealed class ReadOnlyVisitorsApp : SignInOnApp
{
    protected override string VisitorAccess => "read";
}
