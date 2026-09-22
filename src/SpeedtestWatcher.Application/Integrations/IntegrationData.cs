namespace SpeedtestWatcher.Application.Integrations;

public class IntegrationData
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string DisplayName { get; set; } = "Untitled";
    public string Name { get; set; } = string.Empty;
    public string Data { get; set; } = "{}";
    public DateTime? LastActivity { get; set; }
    public bool ActivityFailed { get; set; }
}
