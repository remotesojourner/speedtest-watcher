namespace SpeedtestWatcher.Tests;

public sealed class NoVisitorsApp : SignInOnApp
{
    protected override string VisitorAccess => "none";
}
