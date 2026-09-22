using Microsoft.AspNetCore.Http;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Web.SignIn;

namespace SpeedtestWatcher.UnitTests.Web.SignIn;

public class AccessPolicyTests
{
    private static readonly AuthSnapshot SignInOn = AuthSnapshot.Default with
    {
        Enabled = true,
        Authority = "https://auth.example.com/application/o/speedtest-watcher/",
        ClientId = "speedtest-watcher"
    };

    [Fact]
    public void SignInOff_EveryoneHasFullAccess()
    {
        Assert.Equal(Access.Full, AccessPolicy.Decide(AuthSnapshot.Default, signedIn: false, validApiToken: false));
    }

    [Fact]
    public void DisableAuth_OverridesSavedSettings()
    {
        var overridden = SignInOn with { DisabledByEnvironment = true };

        Assert.False(overridden.IsActive);
        Assert.Equal(Access.Full, AccessPolicy.Decide(overridden, signedIn: false, validApiToken: false));
    }

    [Fact]
    public void SwitchedOnWithoutAProvider_IsNotEnforced()
    {
        var incomplete = AuthSnapshot.Default with { Enabled = true };

        Assert.False(incomplete.IsActive);
        Assert.Equal(Access.Full, AccessPolicy.Decide(incomplete, signedIn: false, validApiToken: false));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void SignedInUsers_AndTheApiToken_GetFullAccess(bool signedIn, bool validApiToken)
    {
        Assert.Equal(Access.Full, AccessPolicy.Decide(SignInOn, signedIn, validApiToken));
    }

    [Fact]
    public void VisitorsWithoutAccess_AreRefused()
    {
        Assert.Equal(Access.None, AccessPolicy.Decide(SignInOn, signedIn: false, validApiToken: false));
    }

    [Fact]
    public void VisitorsWithReadAccess_AreReadOnly()
    {
        var readOnly = SignInOn with { VisitorAccess = VisitorAccess.Read };

        Assert.Equal(Access.ReadOnly, AccessPolicy.Decide(readOnly, signedIn: false, validApiToken: false));
    }

    [Theory]
    [InlineData(Access.Full, Access.Full, true)]
    [InlineData(Access.Full, Access.ReadOnly, true)]
    [InlineData(Access.ReadOnly, Access.ReadOnly, true)]
    [InlineData(Access.ReadOnly, Access.Full, false)]
    [InlineData(Access.None, Access.ReadOnly, false)]
    public void ReadEndpoints_LetReadOnlyVisitorsIn_AndEverythingElseNeedsFullAccess(Access access, Access required, bool allowed)
    {
        Assert.Equal(allowed, AccessPolicy.Allows(access, required));
    }

    [Theory]
    [InlineData("/api/prometheus/metrics", true)]
    [InlineData("/_blazor/negotiate", true)]
    [InlineData("/speedtestHub", false)]
    [InlineData("/history", false)]
    [InlineData("/", false)]
    public void ProgrammaticRequests_GetA401RatherThanARedirect(string path, bool expected)
    {
        Assert.Equal(expected, AccessPolicy.IsProgrammatic(new PathString(path)));
    }
}
