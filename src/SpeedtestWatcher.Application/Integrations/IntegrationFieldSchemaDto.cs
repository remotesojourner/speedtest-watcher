namespace SpeedtestWatcher.Application.Integrations;

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
