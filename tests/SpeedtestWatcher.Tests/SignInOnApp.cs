using SpeedtestWatcher.Application.SignIn;

namespace SpeedtestWatcher.Tests;

public abstract class SignInOnApp : TestApp
{
    public string Token { get; } = ApiToken.Generate();

    protected abstract VisitorAccess VisitorAccess { get; }

    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await SampleData.ChooseOoklaAsync(services, cancellationToken);
        await SampleData.TurnSignInOnAsync(services, VisitorAccess, Token, cancellationToken);
    }
}
