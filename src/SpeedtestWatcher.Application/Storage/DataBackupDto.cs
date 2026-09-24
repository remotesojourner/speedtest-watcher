using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Storage;

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

public class DataBackupOutageRow
{
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

public class DataBackupWatchSessionRow
{
    public DateTime StartedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}

public class DataBackupProbeRoundRow
{
    public DateTime At { get; set; }
    public bool Passed { get; set; }
    public int Answered { get; set; }
    public int Asked { get; set; }
    public double? FastestMilliseconds { get; set; }
    public bool DuringTest { get; set; }
}
