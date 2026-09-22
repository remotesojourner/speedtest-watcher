using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.IntegrationTests.Fixtures;

public sealed class ReadOnlyVisitorsApp : SignInOnApp
{
    protected override VisitorAccess VisitorAccess => VisitorAccess.Read;
}
