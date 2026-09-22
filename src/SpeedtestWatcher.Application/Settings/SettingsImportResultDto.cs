namespace SpeedtestWatcher.Application.Settings;

public class SettingsImportResultDto
{
    public int Settings { get; set; }
    public int Integrations { get; set; }
    public bool Recommendations { get; set; }
    public int Skipped { get; set; }
}
