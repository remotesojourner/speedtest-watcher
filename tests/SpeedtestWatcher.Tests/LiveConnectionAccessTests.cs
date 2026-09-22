using System.Net;

namespace SpeedtestWatcher.Tests;

public sealed class LiveConnectionAccessTests : IClassFixture<ReadOnlyVisitorsApp>, IClassFixture<NoVisitorsApp>
{
    private readonly ReadOnlyVisitorsApp _readOnlyVisitors;
    private readonly NoVisitorsApp _noVisitors;

    public LiveConnectionAccessTests(ReadOnlyVisitorsApp readOnlyVisitors, NoVisitorsApp noVisitors)
    {
        _readOnlyVisitors = readOnlyVisitors;
        _noVisitors = noVisitors;
    }

    [Theory]
    [InlineData("/_blazor/negotiate?negotiateVersion=1")]
    [InlineData("/speedtestHub/negotiate?negotiateVersion=1")]
    public async Task ReadOnlyVisitors_CanOpenTheLiveConnections(string url)
    {
        using var client = _readOnlyVisitors.CreateClientWithoutRedirects();

        using var response = await client.PostAsync(url, null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/_blazor/negotiate?negotiateVersion=1")]
    [InlineData("/speedtestHub/negotiate?negotiateVersion=1")]
    public async Task VisitorsWithoutAccess_CannotOpenThem(string url)
    {
        using var client = _noVisitors.CreateClientWithoutRedirects();

        using var response = await client.PostAsync(url, null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
