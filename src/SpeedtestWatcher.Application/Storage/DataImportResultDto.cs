namespace SpeedtestWatcher.Application.Storage;

public class DataImportResultDto
{
    public int Speedtests { get; set; }
    public int Outages { get; set; }
    public int WatchSessions { get; set; }
    public int ProbeRounds { get; set; }
    public int Skipped { get; set; }
}
