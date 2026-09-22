using System.Net;
using System.Text.Json.Nodes;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using static SpeedtestWatcher.TestSupport.Approvals;

namespace SpeedtestWatcher.IntegrationTests.Web.Api;

public sealed class OpenApiDocumentTests : IClassFixture<SignInOffApp>
{
    private const string ApprovedSpec = "../../../../docs/openapi.json";

    private static readonly HashSet<string> _reasonPhrases = Enum.GetValues<HttpStatusCode>()
        .Select(code => ReasonPhrase((int)code))
        .Append("OK")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly SignInOffApp _app;

    public OpenApiDocumentTests(SignInOffApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task TheSpecMatchesTheApprovedDocument()
    {
        AssertMatchesApproved(ApprovedSpec, (await SpecTextAsync()).ReplaceLineEndings("\n").TrimEnd() + "\n");
    }

    [Fact]
    public async Task EveryOperationHasASummaryATagAndItsAccessLevel()
    {
        var problems = Operations(await SpecAsync())
            .SelectMany(operation => OperationProblems(operation.Key, operation.Operation))
            .ToList();

        Assert.Empty(problems);
    }

    [Fact]
    public async Task EveryResponseIsDescribedAndEveryOperationDocumentsHowItFails()
    {
        var problems = Operations(await SpecAsync())
            .SelectMany(operation => ResponseProblems(operation.Key, operation.Operation))
            .ToList();

        Assert.Empty(problems);
    }

    [Fact]
    public async Task TheSpecsAccessLevelsMatchTheEnforcedPolicies()
    {
        var readable = Operations(await SpecAsync())
            .Where(operation => (string?)operation.Operation["x-access"] == "read")
            .Select(operation => operation.Key)
            .Order(StringComparer.Ordinal);

        Assert.Equal(ApiAccessTests.VisitorEndpoints.Select(RouteWithoutConstraints).Order(StringComparer.Ordinal), readable);
    }

    [Fact]
    public async Task EveryContractTypeAndPropertyIsDescribed()
    {
        var schemas = (await SpecAsync())["components"]!["schemas"]!.AsObject();
        var problems = new List<string>();

        foreach (var (name, schema) in schemas)
        {
            if (schema!["enum"] != null) continue;
            if (string.IsNullOrWhiteSpace((string?)schema["description"])) problems.Add($"{name} has no description");
            foreach (var (property, value) in schema["properties"]?.AsObject() ?? [])
            {
                if (string.IsNullOrWhiteSpace((string?)value!["description"])) problems.Add($"{name}.{property} has no description");
            }
        }

        Assert.Empty(problems);
    }

    [Fact]
    public async Task TheSpecCoversEveryApiEndpoint()
    {
        var documented = Operations(await SpecAsync()).Select(operation => operation.Key).Order(StringComparer.Ordinal);
        var endpoints = ApiAccessTests.VisitorEndpoints.Concat(ApiAccessTests.FullAccessEndpoints).Select(RouteWithoutConstraints).Order(StringComparer.Ordinal);

        Assert.Equal(endpoints, documented);
    }

    private async Task<string> SpecTextAsync()
    {
        using var client = _app.CreateClientWithoutRedirects();
        using var response = await client.GetAsync("/api/openapi/v1.json", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private async Task<JsonNode> SpecAsync() => JsonNode.Parse(await SpecTextAsync())!;

    private static IEnumerable<(string Key, JsonNode Operation)> Operations(JsonNode spec) =>
        spec["paths"]!.AsObject().SelectMany(path => path.Value!.AsObject()
            .Select(operation => ($"{operation.Key.ToUpperInvariant()} {path.Key}", operation.Value!)));

    private static IEnumerable<string> OperationProblems(string key, JsonNode operation)
    {
        if (string.IsNullOrWhiteSpace((string?)operation["summary"])) yield return $"{key} has no summary";
        if (operation["tags"]?.AsArray().Count != 1) yield return $"{key} needs exactly one tag";
        if ((string?)operation["x-access"] is not ("read" or "full")) yield return $"{key} doesn't say which access it needs";
        if ((string?)operation["description"] is not { } description || !description.Contains("**Access:**", StringComparison.Ordinal))
            yield return $"{key} doesn't describe its access";

        foreach (var parameter in operation["parameters"]?.AsArray() ?? [])
        {
            if (string.IsNullOrWhiteSpace((string?)parameter!["description"])) yield return $"{key} parameter {parameter["name"]} has no description";
        }

        if (operation["requestBody"] is { } body && string.IsNullOrWhiteSpace((string?)body["description"]))
            yield return $"{key} doesn't describe its request body";
    }

    private static IEnumerable<string> ResponseProblems(string key, JsonNode operation)
    {
        var responses = operation["responses"]!.AsObject();
        if (!responses.Any(response => response.Key.StartsWith('2'))) yield return $"{key} documents no success response";
        if (!responses.Any(response => response.Key.StartsWith('4'))) yield return $"{key} documents no error responses";

        foreach (var (status, response) in responses)
        {
            if ((string?)response!["description"] is not { } description || _reasonPhrases.Contains(description))
                yield return $"{key} {status} has no written description";
        }
    }

    private static string RouteWithoutConstraints(string endpoint) => endpoint.Replace(":int", "", StringComparison.Ordinal);

    private static string ReasonPhrase(int status) => new HttpResponseMessage((HttpStatusCode)status).ReasonPhrase ?? "";
}
