using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Recommendations;

namespace SpeedtestWatcher.Application.Settings;

public class SettingsBackupDto
{
    public List<ConfigEntry> Config { get; set; } = [];
    public List<IntegrationData> Integrations { get; set; } = [];
    public Recommendation? Recommendations { get; set; }
}
