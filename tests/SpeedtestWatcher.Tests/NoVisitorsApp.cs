using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Tests;

public sealed class NoVisitorsApp : SignInOnApp
{
    protected override VisitorAccess VisitorAccess => VisitorAccess.None;
}
