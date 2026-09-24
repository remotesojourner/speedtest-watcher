using System.Collections;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Web.Api;

public sealed partial class ApiAccessTests : IClassFixture<ReadOnlyVisitorsApp>, IClassFixture<NoVisitorsApp>
{
    internal static IReadOnlyList<string> VisitorEndpoints { get; } =
    [
        "GET /api/config",
        "GET /api/monitoring/days",
        "GET /api/monitoring/latency",
        "GET /api/monitoring/outages",
        "GET /api/monitoring/status",
        "GET /api/opengraph/image",
        "GET /api/prometheus/metrics",
        "GET /api/speedtests",
        "GET /api/speedtests/statistics",
        "GET /api/speedtests/status",
        "GET /api/speedtests/{id:int}",
        "POST /api/speedtests/export"
    ];

    internal static IReadOnlyList<string> FullAccessEndpoints { get; } =
    [
        "DELETE /api/monitoring/outages/{id:int}",
        "DELETE /api/speedtests/{id:int}",
        "DELETE /api/storage/data",
        "DELETE /api/storage/tests/history",
        "GET /api/info/version",
        "GET /api/recommendations",
        "GET /api/storage",
        "GET /api/storage/config",
        "GET /api/storage/data",
        "PATCH /api/config",
        "POST /api/speedtests/continue",
        "POST /api/speedtests/pause",
        "POST /api/speedtests/run",
        "PUT /api/storage/config",
        "PUT /api/storage/data"
    ];

    private static readonly string[] _documentationPages = ["/api/openapi/v1.json", "/api/docs/"];

    private static readonly Dictionary<string, string> _sampleRouteValues = new()
    {
        ["id:int"] = "1"
    };

    private readonly ReadOnlyVisitorsApp _readOnlyVisitors;
    private readonly NoVisitorsApp _noVisitors;

    public ApiAccessTests(ReadOnlyVisitorsApp readOnlyVisitors, NoVisitorsApp noVisitors)
    {
        _readOnlyVisitors = readOnlyVisitors;
        _noVisitors = noVisitors;
    }

    public static TheoryData<string> ReadableByVisitors => new(VisitorEndpoints);

    public static TheoryData<string> FullAccessOnly => new(FullAccessEndpoints);

    public static TheoryData<string> Documentation => new(_documentationPages);

    public static TheoryData<string> FullAccessReadsAndAHarmlessWrite =>
        new(FullAccessEndpoints.Where(endpoint => endpoint.StartsWith("GET ", StringComparison.Ordinal)).Append("POST /api/speedtests/continue"));

    [Fact]
    public void EveryApiEndpointHasADecidedAccessLevel()
    {
        var decided = VisitorEndpoints.Concat(FullAccessEndpoints).Order(StringComparer.Ordinal);
        var discovered = ApiEndpoints(_readOnlyVisitors).Keys.Order(StringComparer.Ordinal);

        Assert.Equal(decided, discovered);
    }

    [Theory]
    [MemberData(nameof(ReadableByVisitors))]
    public async Task ReadOnlyVisitorsCanUseTheReadEndpoints(string endpoint)
    {
        using var response = await SendAsync(_readOnlyVisitors, endpoint);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True((int)response.StatusCode < 500, $"{endpoint} answered {(int)response.StatusCode}");
    }

    [Theory]
    [MemberData(nameof(FullAccessOnly))]
    public async Task ReadOnlyVisitorsAreRefusedEverythingElse(string endpoint)
    {
        using var response = await SendAsync(_readOnlyVisitors, endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ReadableByVisitors))]
    [MemberData(nameof(FullAccessOnly))]
    public async Task VisitorsWithoutAccessAreRefusedEveryEndpoint(string endpoint)
    {
        using var response = await SendAsync(_noVisitors, endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Theory]
    [MemberData(nameof(FullAccessReadsAndAHarmlessWrite))]
    public async Task TheApiTokenGivesFullAccess(string endpoint)
    {
        using var response = await SendAsync(_readOnlyVisitors, endpoint, _readOnlyVisitors.Token);

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound, $"{endpoint} answered {(int)response.StatusCode}");
    }

    [Theory]
    [MemberData(nameof(Documentation))]
    public async Task TheSpecAndTheReferencePageAreOpenToReadOnlyVisitors(string path)
    {
        using var client = _readOnlyVisitors.CreateClientWithoutRedirects();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Documentation))]
    public async Task TheSpecAndTheReferencePageRefuseVisitorsWithoutAccess(string path)
    {
        using var client = _noVisitors.CreateClientWithoutRedirects();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task AWrongApiTokenIsRefused()
    {
        using var response = await SendAsync(_readOnlyVisitors, "GET /api/storage", "swt_not-the-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PagesSendVisitorsWithoutAccessToSignIn()
    {
        using var client = _noVisitors.CreateClientWithoutRedirects();

        using var response = await client.GetAsync("/history", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/auth/login?returnUrl=%2Fhistory", response.Headers.Location?.OriginalString);
    }

    private static async Task<HttpResponseMessage> SendAsync(TestApp app, string endpoint, string? bearerToken = null)
    {
        var action = ApiEndpoints(app)[endpoint];
        var (method, route) = (endpoint[..endpoint.IndexOf(' ')], endpoint[(endpoint.IndexOf(' ') + 1)..]);

        using var request = new HttpRequestMessage(new HttpMethod(method), RouteParameter().Replace(route, parameter => _sampleRouteValues[parameter.Groups[1].Value]));
        if (BodyType(action) is { } bodyType)
            request.Content = new StringContent(IsList(bodyType) ? "[]" : "{}", Encoding.UTF8, "application/json");
        if (bearerToken != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var client = app.CreateClientWithoutRedirects();
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static Dictionary<string, ControllerActionDescriptor> ApiEndpoints(TestApp app) =>
        app.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => (Route: "/" + endpoint.RoutePattern.RawText?.TrimStart('/'), Endpoint: endpoint, Action: endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()))
            .Where(entry => entry.Action != null && entry.Route.StartsWith("/api/", StringComparison.Ordinal))
            .SelectMany(entry => entry.Endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods
                .Select(method => (Key: $"{method} {entry.Route}", entry.Action)))
            .ToDictionary(entry => entry.Key, entry => entry.Action!);

    private static Type? BodyType(ControllerActionDescriptor action) =>
        action.Parameters.FirstOrDefault(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)?.ParameterType;

    private static bool IsList(Type type) =>
        type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type);

    [GeneratedRegex(@"\{([^}]+)\}")]
    private static partial Regex RouteParameter();
}
