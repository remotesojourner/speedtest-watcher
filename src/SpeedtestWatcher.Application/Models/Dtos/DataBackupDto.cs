namespace SpeedtestWatcher.Application.Models.Dtos;

public class DataBackupDto
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public DateTime Exported { get; set; } = DateTime.UtcNow;
    public List<SpeedtestImportRow> Speedtests { get; set; } = [];
    public List<DataBackupOutageRow> Outages { get; set; } = [];
    public List<DataBackupWatchSessionRow> WatchSessions { get; set; } = [];
    public List<DataBackupProbeRoundRow> ProbeRounds { get; set; } = [];

    public bool IsEmpty => Speedtests.Count == 0 && Outages.Count == 0 && WatchSessions.Count == 0 && ProbeRounds.Count == 0;
}
