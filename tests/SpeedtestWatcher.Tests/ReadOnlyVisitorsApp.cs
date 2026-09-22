using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Tests;

public sealed class ReadOnlyVisitorsApp : SignInOnApp
{
    protected override VisitorAccess VisitorAccess => VisitorAccess.Read;
}
