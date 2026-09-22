using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Api.OpenApi;

public static class ApiDocumentation
{
    public const string DocumentName = "v1";
    public const string DocumentRoute = "/api/openapi/{documentName}.json";
    public const string ReferenceRoute = "/api/docs";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
        return services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer<ApiDocumentTransformer>();
            options.AddOperationTransformer<AccessOperationTransformer>();
            options.AddSchemaTransformer<ConfigSchemaTransformer>();
        });
    }

    public static IEndpointRouteBuilder MapApiDocumentation(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpenApi(DocumentRoute).RequireAuthorization(AccessPolicies.Read);
        endpoints.MapScalarApiReference(ReferenceRoute, options => options
                .WithTitle(ApiDocumentTransformer.Title)
                .WithOpenApiRoutePattern(DocumentRoute)
                .WithFavicon("/img/logo.svg")
                .DisableDefaultFonts()
                .DisableTelemetry()
                .DisableAgent()
                .DisableMcp()
                .HideDeveloperTools())
            .RequireAuthorization(AccessPolicies.Read);
        return endpoints;
    }
}
