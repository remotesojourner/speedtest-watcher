using Microsoft.AspNetCore.Http;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Web.SignIn;

namespace SpeedtestWatcher.UnitTests.Application.SignIn;

public class AccessTokenTests
{
    [Fact]
    public void ApiTokenMatchesOnlyItsOwnHash()
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
    public void ApiTokenIsReadFromABearerHeader()
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
    public void ScopesAlwaysIncludeOpenIdWithoutDuplicates(string raw, string[] expected)
    {
        Assert.Equal(expected, SignInSettings.ParseScopes(raw));
    }
}
