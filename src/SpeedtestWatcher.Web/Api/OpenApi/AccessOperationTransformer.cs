using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using SpeedtestWatcher.Web.Api.Contracts;
using SpeedtestWatcher.Web.SignIn;

namespace SpeedtestWatcher.Web.Api.OpenApi;

public sealed class AccessOperationTransformer : IOpenApiOperationTransformer
{
    public const string AccessExtension = "x-access";

    private const string ReadAccess =
        "**Access:** read. Works for everyone while sign-in is off, and for read-only visitors, signed-in users and API tokens while it's on.";

    private const string FullAccess =
        "**Access:** full. Needs sign-in to be off, a signed-in browser session, or an API token.";

    private const string ReadRefused =
        "Sign-in is on, people who aren't signed in have no access, and the request has no valid API token or signed-in session.";

    private const string FullRefused =
        "Sign-in is on and the request has no valid API token or signed-in session.";

    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var readable = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any(data => data.Policy == AccessPolicies.Read);

        operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        operation.Extensions[AccessExtension] = new JsonNodeExtension(JsonValue.Create(readable ? "read" : "full"));

        var access = readable ? ReadAccess : FullAccess;
        var remarks = operation.Description?.ReplaceLineEndings("\n").TrimEnd();
        operation.Description = string.IsNullOrWhiteSpace(remarks) ? access : $"{remarks}\n\n{access}";

        var errorSchema = await context.GetOrCreateSchemaAsync(typeof(ErrorResponse), null, cancellationToken);
        context.Document?.AddComponent(nameof(ErrorResponse), errorSchema);

        operation.Responses ??= new OpenApiResponses();
        operation.Responses[StatusCodes.Status401Unauthorized.ToString(System.Globalization.CultureInfo.InvariantCulture)] = new OpenApiResponse
        {
            Description = readable ? ReadRefused : FullRefused,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new() { Schema = new OpenApiSchemaReference(nameof(ErrorResponse), context.Document) }
            }
        };
    }
}
