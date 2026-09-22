using Microsoft.AspNetCore.Http;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Web.SignIn;

namespace SpeedtestWatcher.UnitTests.Web.SignIn;

public class AccessPolicyTests
{
    private static readonly AuthSnapshot _signInOn = AuthSnapshot.Default with
    {
        Enabled = true,
        Authority = "https://auth.example.com/application/o/speedtest-watcher/",
        ClientId = "speedtest-watcher"
    };

    [Fact]
    public void SignInOffEveryoneHasFullAccess()
    {
        Assert.Equal(Access.Full, AccessPolicy.Decide(AuthSnapshot.Default, signedIn: false, validApiToken: false));
    }

    [Fact]
    public void DisableAuthOverridesSavedSettings()
    {
        var overridden = _signInOn with { DisabledByEnvironment = true };

        Assert.False(overridden.IsActive);
        Assert.Equal(Access.Full, AccessPolicy.Decide(overridden, signedIn: false, validApiToken: false));
    }

    [Fact]
    public void SwitchedOnWithoutAProviderIsNotEnforced()
    {
        var incomplete = AuthSnapshot.Default with { Enabled = true };

        Assert.False(incomplete.IsActive);
        Assert.Equal(Access.Full, AccessPolicy.Decide(incomplete, signedIn: false, validApiToken: false));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void SignedInUsersAndTheApiTokenGetFullAccess(bool signedIn, bool validApiToken)
    {
        Assert.Equal(Access.Full, AccessPolicy.Decide(_signInOn, signedIn, validApiToken));
    }

    [Fact]
    public void VisitorsWithoutAccessAreRefused()
    {
        Assert.Equal(Access.None, AccessPolicy.Decide(_signInOn, signedIn: false, validApiToken: false));
    }

    [Fact]
    public void VisitorsWithReadAccessAreReadOnly()
    {
        var readOnly = _signInOn with { VisitorAccess = VisitorAccess.Read };

        Assert.Equal(Access.ReadOnly, AccessPolicy.Decide(readOnly, signedIn: false, validApiToken: false));
    }

    [Theory]
    [InlineData(Access.Full, Access.Full, true)]
    [InlineData(Access.Full, Access.ReadOnly, true)]
    [InlineData(Access.ReadOnly, Access.ReadOnly, true)]
    [InlineData(Access.ReadOnly, Access.Full, false)]
    [InlineData(Access.None, Access.ReadOnly, false)]
    public void ReadEndpointsLetReadOnlyVisitorsInAndEverythingElseNeedsFullAccess(Access access, Access required, bool allowed)
    {
        Assert.Equal(allowed, AccessPolicy.Allows(access, required));
    }

    [Theory]
    [InlineData("/api/prometheus/metrics", true)]
    [InlineData("/_blazor/negotiate", true)]
    [InlineData("/speedtestHub", false)]
    [InlineData("/history", false)]
    [InlineData("/", false)]
    public void ProgrammaticRequestsGetA401RatherThanARedirect(string path, bool expected)
    {
        Assert.Equal(expected, AccessPolicy.IsProgrammatic(new PathString(path)));
    }
}
