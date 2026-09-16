using Microsoft.AspNetCore.Http;
using SpeedtestWatcher.Web.Services.Auth;

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
        Assert.Equal(Access.Full, AccessPolicy.Decide(AuthSnapshot.Default, signedIn: false, internalCall: false, validApiToken: false));
    }

    [Fact]
    public void DisableAuth_OverridesSavedSettings()
    {
        var overridden = SignInOn with { DisabledByEnvironment = true };

        Assert.False(overridden.IsActive);
        Assert.Equal(Access.Full, AccessPolicy.Decide(overridden, signedIn: false, internalCall: false, validApiToken: false));
    }

    [Fact]
    public void SwitchedOnWithoutAProvider_IsNotEnforced()
    {
        var incomplete = AuthSnapshot.Default with { Enabled = true };

        Assert.False(incomplete.IsActive);
        Assert.Equal(Access.Full, AccessPolicy.Decide(incomplete, signedIn: false, internalCall: false, validApiToken: false));
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void SignedInUsers_TheUisOwnCalls_AndTheApiToken_GetFullAccess(bool signedIn, bool internalCall, bool validApiToken)
    {
        Assert.Equal(Access.Full, AccessPolicy.Decide(SignInOn, signedIn, internalCall, validApiToken));
    }

    [Fact]
    public void VisitorsWithoutAccess_AreRefused()
    {
        Assert.Equal(Access.None, AccessPolicy.Decide(SignInOn, signedIn: false, internalCall: false, validApiToken: false));
    }

    [Fact]
    public void VisitorsWithReadAccess_AreReadOnly()
    {
        var readOnly = SignInOn with { VisitorAccess = "read" };

        Assert.Equal(Access.ReadOnly, AccessPolicy.Decide(readOnly, signedIn: false, internalCall: false, validApiToken: false));
    }

    [Theory]
    [InlineData("/auth/login", true)]
    [InlineData("/auth/failed", true)]
    [InlineData("/signin-oidc", true)]
    [InlineData("/authorize", false)]
    [InlineData("/api/config", false)]
    [InlineData("/settings/security", false)]
    public void OnlySignInPaths_AreAlwaysReachable(string path, bool expected)
    {
        Assert.Equal(expected, AccessPolicy.IsPublic(new PathString(path)));
    }

    [Theory]
    [InlineData("/api/prometheus/metrics", true)]
    [InlineData("/_blazor/negotiate", true)]
    [InlineData("/speedtestHub", true)]
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
        Assert.Equal("swt_abc", ApiToken.FromRequest(context.Request));

        context.Request.Headers.Authorization = "Basic cHJvbWV0aGV1czpzZWNyZXQ=";
        Assert.Null(ApiToken.FromRequest(context.Request));
    }

    [Fact]
    public void InternalToken_IsDifferentForEachProcessInstance_AndMatchesOnlyItself()
    {
        var first = new InternalAccessToken();
        var second = new InternalAccessToken();

        Assert.NotEqual(first.Value, second.Value);
        Assert.True(first.Matches(first.Value));
        Assert.False(first.Matches(second.Value));
        Assert.False(first.Matches(null));
    }

    [Theory]
    [InlineData("profile email", new[] { "openid", "profile", "email" })]
    [InlineData("openid,email openid", new[] { "openid", "email" })]
    [InlineData("", new[] { "openid" })]
    public void Scopes_AlwaysIncludeOpenId_WithoutDuplicates(string raw, string[] expected)
    {
        Assert.Equal(expected, AuthSettings.ParseScopes(raw));
    }
}
