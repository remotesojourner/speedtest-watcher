using System.Text.Json.Serialization;

namespace SpeedtestWatcher.Core.DTOs;

public class IntegrationFieldSchemaDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("regex")]
    public string? Regex { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; set; }

    [JsonPropertyName("default")]
    public object? Default { get; set; }
}

public class IntegrationTypeSchemaDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public List<IntegrationFieldSchemaDto> Fields { get; set; } = [];
}

public class ActiveIntegrationDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public Dictionary<string, object?> Data { get; set; } = new();

    [JsonPropertyName("lastActivity")]
    public DateTime? LastActivity { get; set; }

    [JsonPropertyName("activityFailed")]
    public bool ActivityFailed { get; set; }
}

public class IntegrationTestResultDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
