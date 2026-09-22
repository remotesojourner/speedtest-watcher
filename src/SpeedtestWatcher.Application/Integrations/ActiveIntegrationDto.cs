namespace SpeedtestWatcher.Application.Integrations;

public class ActiveIntegrationDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object?> Data { get; set; } = new();
    public DateTime? LastActivity { get; set; }
    public bool ActivityFailed { get; set; }
}
