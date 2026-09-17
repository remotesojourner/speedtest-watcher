using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Core.DTOs;

public class SettingsBackupDto
{
    public List<ConfigEntry> Config { get; set; } = [];
    public List<IntegrationData> Integrations { get; set; } = [];
    public Recommendation? Recommendations { get; set; }
}
