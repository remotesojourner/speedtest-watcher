using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.IntegrationTests.Fixtures;

public sealed class NoVisitorsApp : SignInOnApp
{
    protected override VisitorAccess VisitorAccess => VisitorAccess.None;
}
