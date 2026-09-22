using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Web.Api.OpenApi;

public sealed class ApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public const string Title = "Speedtest Watcher API";
    public const string SecuritySchemeName = "apiToken";

    private const string FileSchemaId = "Stream";

    private const string Description = $$"""
        Results, statistics, settings, backups and metrics of a Speedtest Watcher instance.

        - **Authentication:** while sign-in is off, no token is needed. While it's on, create a token under **Settings → Security → API Token** and send it as `Authorization: Bearer swt_…`. Read operations also work without a token when people who aren't signed in have read-only access.
        - **Errors** answer with `{ "message": "…" }` and a 4xx status. `401` means the request needs a valid token.
        - **Units:** timestamps are UTC ISO 8601, speeds are Mbps, ping and jitter are milliseconds, and test durations are seconds. Failed and skipped results have `-1` readings.

        The guide, with examples, is at {{ProjectInfo.RepositoryUrl}}/blob/main/docs/api.md.
        """;

    private const string SecuritySchemeDescription =
        "An API token (`swt_…`) from **Settings → Security → API Token**. It's needed only while sign-in is on, and read operations don't need it when people who aren't signed in have read-only access.";

    private static readonly (string Name, string Description)[] _tags =
    [
        ("Speedtests", "Stored results and statistics, and running and pausing tests."),
        ("Settings", "Reading and changing settings."),
        ("Recommendations", "Suggested targets based on recent results."),
        ("Storage", "The database, bulk result exports and imports, and settings backups."),
        ("Prometheus", "Metrics for Prometheus."),
        ("Link preview", "The image shown when a link to Speedtest Watcher is shared."),
        ("Info", "Information about this instance.")
    ];

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = Title,
            Version = ProjectInfo.Version,
            Description = Description,
            License = new OpenApiLicense { Name = "AGPL-3.0-only", Identifier = "AGPL-3.0-only" }
        };
        document.Servers = [];

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            [SecuritySchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                Description = SecuritySchemeDescription
            }
        };
        document.Security =
        [
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(SecuritySchemeName, document)] = [] },
            new OpenApiSecurityRequirement()
        ];

        var used = document.Paths.Values
            .SelectMany(path => path.Operations?.Values.AsEnumerable() ?? [])
            .SelectMany(operation => operation.Tags?.AsEnumerable() ?? [])
            .Select(tag => tag.Name)
            .ToHashSet(StringComparer.Ordinal);
        document.Tags = new HashSet<OpenApiTag>(_tags
            .Where(tag => used.Contains(tag.Name))
            .Select(tag => new OpenApiTag { Name = tag.Name, Description = tag.Description }));

        InlineFileSchemas(document);
        DescribeNullablePropertiesAtTheTop(document);
        return Task.CompletedTask;
    }

    private static void DescribeNullablePropertiesAtTheTop(OpenApiDocument document)
    {
        var properties = (document.Components?.Schemas?.Values.AsEnumerable() ?? [])
            .SelectMany(schema => schema.Properties?.Values.AsEnumerable() ?? [])
            .OfType<OpenApiSchema>()
            .Where(property => string.IsNullOrWhiteSpace(property.Description) && property.OneOf is { Count: > 0 });
        foreach (var property in properties)
        {
            if (property.OneOf!.FirstOrDefault(branch => !string.IsNullOrWhiteSpace(branch.Description)) is not { } described) continue;

            property.Description = described.Description;
            described.Description = null;
        }
    }

    private static void InlineFileSchemas(OpenApiDocument document)
    {
        var fileContents = document.Paths.Values
            .SelectMany(path => path.Operations?.Values.AsEnumerable() ?? [])
            .SelectMany(operation => operation.Responses?.Values.AsEnumerable() ?? [])
            .SelectMany(response => response.Content?.Values.AsEnumerable() ?? [])
            .Where(content => content.Schema is OpenApiSchemaReference { Reference.Id: FileSchemaId });
        foreach (var content in fileContents)
            content.Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" };

        document.Components?.Schemas?.Remove(FileSchemaId);
    }
}
