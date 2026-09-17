namespace SpeedtestWatcher.Core.DTOs;

public class IntegrationFieldSchemaDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public string Description { get; set; } = string.Empty;
    public string? Regex { get; set; }
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
    public object? Default { get; set; }
}

public class IntegrationTypeSchemaDto
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<IntegrationFieldSchemaDto> Fields { get; set; } = [];
}

public class ActiveIntegrationDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object?> Data { get; set; } = new();
    public DateTime? LastActivity { get; set; }
    public bool ActivityFailed { get; set; }
}

public class IntegrationTestResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
