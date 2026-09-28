using SpeedtestWatcher.Application.Models.Entities;

namespace SpeedtestWatcher.Application.Models.Dtos;

public class SettingsBackupDto
{
    public List<ConfigEntry> Config { get; set; } = [];
    public List<IntegrationData> Integrations { get; set; } = [];
    public Recommendation? Recommendations { get; set; }
}
