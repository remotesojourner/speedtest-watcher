using Microsoft.AspNetCore.Http;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Web.SignIn;

namespace SpeedtestWatcher.Tests;

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

public class AccessTokenTests
{
    [Fact]
    public void ApiToken_MatchesOnlyItsOwnHash()
    {
        var token = ApiToken.Generate();
        var hash = ApiToken.Hash(token);

        Assert.StartsWith("swt_", token);
        Assert.True(ApiToken.Matches(token, hash));
        Assert.False(ApiToken.Matches(ApiToken.Generate(), hash));
        Assert.False(ApiToken.Matches(token, null));
        Assert.False(ApiToken.Matches(null, hash));
    }

    [Fact]
    public void ApiToken_IsReadFromABearerHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer swt_abc";
        Assert.Equal("swt_abc", BearerToken.FromRequest(context.Request));

        context.Request.Headers.Authorization = "Basic cHJvbWV0aGV1czpzZWNyZXQ=";
        Assert.Null(BearerToken.FromRequest(context.Request));
    }

    [Theory]
    [InlineData("profile email", new[] { "openid", "profile", "email" })]
    [InlineData("openid,email openid", new[] { "openid", "email" })]
    [InlineData("", new[] { "openid" })]
    public void Scopes_AlwaysIncludeOpenId_WithoutDuplicates(string raw, string[] expected)
    {
        Assert.Equal(expected, SignInSettings.ParseScopes(raw));
    }
}
