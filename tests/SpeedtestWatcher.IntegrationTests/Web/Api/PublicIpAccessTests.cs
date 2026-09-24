using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.Web.Api.Contracts;

namespace SpeedtestWatcher.IntegrationTests.Web.Api;

public sealed class PublicIpAccessTests : IClassFixture<ReadOnlyVisitorsApp>
{
    private readonly ReadOnlyVisitorsApp _app;

    public PublicIpAccessTests(ReadOnlyVisitorsApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task AReadOnlyVisitorSeesEveryFigureExceptThePublicIp()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var owner = _app.CreateClientWithoutRedirects();
        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _app.Token);
        using var imported = await owner.PutAsJsonAsync("/api/storage/data", new DataBackup
        {
            Speedtests =
            [
                new DataBackupSpeedtest
                {
                    Created = new DateTime(2026, 9, 22, 9, 15, 0, DateTimeKind.Utc),
                    Status = "completed", Ping = 14, Download = 941.2, Upload = 108.7,
                    PacketLoss = 0, DownloadBytes = 903347628, UploadBytes = 88429797
                }
            ]
        }, cancellationToken);
        imported.EnsureSuccessStatusCode();

        using var visitor = _app.CreateClientWithoutRedirects();
        var asVisitor = await ResultAsync(visitor, cancellationToken);

        Assert.Equal(903347628, asVisitor.GetProperty("downloadBytes").GetInt64());
        Assert.Equal(0, asVisitor.GetProperty("packetLoss").GetDouble());
        Assert.Equal(JsonValueKind.Null, asVisitor.GetProperty("publicIp").ValueKind);
    }

    private static async Task<JsonElement> ResultAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var results = JsonDocument.Parse(await client.GetStringAsync("/api/speedtests?limit=1", cancellationToken));
        return results.RootElement[0].Clone();
    }
}
