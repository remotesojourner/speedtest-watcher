using System.Net;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Web.SignIn;

public sealed class LiveConnectionAccessTests : IClassFixture<ReadOnlyVisitorsApp>, IClassFixture<NoVisitorsApp>
{
    private readonly ReadOnlyVisitorsApp _readOnlyVisitors;
    private readonly NoVisitorsApp _noVisitors;

    public LiveConnectionAccessTests(ReadOnlyVisitorsApp readOnlyVisitors, NoVisitorsApp noVisitors)
    {
        _readOnlyVisitors = readOnlyVisitors;
        _noVisitors = noVisitors;
    }

    private const string CircuitNegotiation = "/_blazor/negotiate?negotiateVersion=1";

    [Fact]
    public async Task ReadOnlyVisitors_CanOpenTheLiveConnection()
    {
        using var client = _readOnlyVisitors.CreateClientWithoutRedirects();

        using var response = await client.PostAsync(CircuitNegotiation, null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VisitorsWithoutAccess_CannotOpenIt()
    {
        using var client = _noVisitors.CreateClientWithoutRedirects();

        using var response = await client.PostAsync(CircuitNegotiation, null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TheOldSpeedtestHub_IsGone()
    {
        using var client = _readOnlyVisitors.CreateClientWithoutRedirects();

        using var response = await client.PostAsync("/speedtestHub/negotiate?negotiateVersion=1", null, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
