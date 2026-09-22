using System.Net;
using System.Text.RegularExpressions;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.Web.Api;

namespace SpeedtestWatcher.IntegrationTests.Web.Ui;

public sealed partial class LinkPreviewTagsTests : IClassFixture<SignInOffApp>
{
    private readonly SignInOffApp _app;

    public LinkPreviewTagsTests(SignInOffApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task EveryPageNamesThePreviewImageByItsFullAddress()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClientWithoutRedirects();

        var image = await PreviewImageAsync(client, new HttpRequestMessage(HttpMethod.Get, "/history"), cancellationToken);

        Assert.Equal(new Uri(client.BaseAddress!, OpenGraphController.ImagePath).ToString(), image);
        using var response = await client.GetAsync(image, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task BehindAProxyTheImageAddressIsThePublicOne()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClientWithoutRedirects();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/history");
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-Host", "speed.example.com");

        var image = await PreviewImageAsync(client, request, cancellationToken);

        Assert.Equal($"https://speed.example.com/{OpenGraphController.ImagePath}", image);
    }

    private static async Task<string> PreviewImageAsync(HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await client.SendAsync(request, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var match = OpenGraphImage().Match(await response.Content.ReadAsStringAsync(cancellationToken));
        Assert.True(match.Success, "The page has no og:image tag");
        return match.Groups["url"].Value;
    }

    [GeneratedRegex("""<meta property="og:image" content="(?<url>[^"]+)" />""")]
    private static partial Regex OpenGraphImage();
}
