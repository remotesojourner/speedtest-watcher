using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Web.Api.Contracts;

namespace SpeedtestWatcher.Web.Api.OpenApi;

public sealed class ConfigSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type != typeof(ConfigResponse)) return Task.CompletedTask;

        schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
        schema.Properties.Remove("settings");
        foreach (var definition in SettingDefinitions.All.Where(definition => definition.Visibility != SettingVisibility.Secret))
        {
            schema.Properties[definition.Key] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Description = Describe(definition),
                Default = JsonValue.Create(definition.Default)
            };
        }

        schema.AdditionalProperties = null;
        return Task.CompletedTask;
    }

    private static string Describe(SettingDefinition definition) => definition.Visibility switch
    {
        SettingVisibility.FullAccessOnly => "Left out for read-only visitors.",
        SettingVisibility.SecurityTab => "Changed only on the Security tab. Left out for read-only visitors.",
        _ => "Visible to everyone who can read the settings."
    };
}
