using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using SpeedtestWatcher.Application.Enums;

namespace SpeedtestWatcher.Web.OpenApi;

internal sealed class SpeedtestQuerySchemaTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Document is not { } document || operation.Parameters is not { } parameters) return;

        var enumQueries = parameters.OfType<OpenApiParameter>()
            .Where(parameter => parameter is { In: ParameterLocation.Query, Schema: OpenApiSchema { Enum.Count: > 0 } });
        foreach (var parameter in enumQueries)
        {
            var description = context.Description.ParameterDescriptions.FirstOrDefault(description =>
                description.Source == BindingSource.Query && description.Name == parameter.Name);
            if (description?.Type is not { } parameterType) continue;

            var enumType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
            if (enumType != typeof(TestStatus) && enumType != typeof(TestType)) continue;

            // MVC binds these query values case-insensitively, so document the same spelling as JSON.
            var schema = await context.GetOrCreateSchemaAsync(enumType, cancellationToken: cancellationToken);
            document.AddComponent(enumType.Name, schema);
            parameter.Schema = new OpenApiSchemaReference(enumType.Name, document);
        }
    }
}
