using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using static SpeedtestWatcher.TestSupport.Approvals;

namespace SpeedtestWatcher.IntegrationTests.Web.Api;

public sealed class ApiResponseTests : IClassFixture<SignInOffApp>
{
    private static readonly (string Method, string Url, string? Body)[] Requests =
    [
        ("GET", "/api/config", null),
        ("GET", "/api/speedtests", null),
        ("GET", "/api/speedtests/2", null),
        ("GET", "/api/speedtests/99", null),
        ("GET", "/api/speedtests?status=unknown", null),
        ("GET", "/api/speedtests/status", null),
        ("GET", "/api/speedtests/statistics?from=2026-09-14&to=2026-09-15&tz=America/New_York", null),
        ("GET", "/api/speedtests/statistics?tz=Mars/Olympus_Mons", null),
        ("POST", "/api/speedtests/export", """{"format":"json"}"""),
        ("POST", "/api/speedtests/export", """{"format":"csv"}"""),
        ("POST", "/api/speedtests/export", """{"format":"""),
        ("GET", "/api/prometheus/metrics", null)
    ];

    private static readonly JsonSerializerOptions ReadableJson = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly SignInOffApp _app;

    public ApiResponseTests(SignInOffApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task ReadEndpoints_AnswerWithTheApprovedResponses()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClientWithoutRedirects();
        var transcript = new StringBuilder();

        foreach (var (method, url, body) in Requests)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (body != null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            transcript.Append("### ").Append(method).Append(' ').Append(url);
            if (body != null) transcript.Append(' ').Append(body);
            transcript.Append('\n')
                .Append(((int)response.StatusCode).ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(response.Content.Headers.ContentType?.ToString()).Append('\n')
                .Append(Readable(content, response.Content.Headers.ContentType?.MediaType)).Append("\n\n");
        }

        AssertMatchesApproved("ApiResponses.approved.txt", transcript.ToString());
    }

    private static string Readable(string content, string? mediaType) =>
        mediaType == "application/json"
            ? Normalized(JsonNode.Parse(content))?.ToJsonString(ReadableJson) ?? "null"
            : content.ReplaceLineEndings("\n").TrimEnd();

    private static JsonNode? Normalized(JsonNode? node) => node switch
    {
        JsonObject properties => new JsonObject(properties
            .OrderBy(property => property.Key, StringComparer.Ordinal)
            .Select(property => KeyValuePair.Create(property.Key, Normalized(property.Value)))),
        JsonArray items => new JsonArray(items.Select(Normalized).ToArray()),
        _ => node?.DeepClone()
    };
}
