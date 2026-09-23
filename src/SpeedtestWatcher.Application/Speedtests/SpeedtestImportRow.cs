using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Speedtests;

public class SpeedtestImportRow
{
    public int ServerId { get; set; }
    public string? ServerName { get; set; }
    public string? ServerHost { get; set; }
    public int Ping { get; set; }
    public double? Jitter { get; set; }
    public double Download { get; set; }
    public double Upload { get; set; }
    public string? Error { get; set; }
    public double? PacketLoss { get; set; }

    public double? BufferbloatDown { get; set; }

    public double? BufferbloatUp { get; set; }
    public long? DownloadBytes { get; set; }
    public long? UploadBytes { get; set; }
    public string? Status { get; set; }
    public bool? Healthy { get; set; }
    public int? ThresholdPing { get; set; }
    public double? ThresholdDownload { get; set; }
    public double? ThresholdUpload { get; set; }
    public string? Type { get; set; }
    public string? ResultId { get; set; }
    public int Time { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;

    public Speedtest ToSpeedtest() => new()
    {
        ServerId = ServerId,
        ServerName = ServerName,
        ServerHost = ServerHost,
        Ping = Ping,
        Jitter = Jitter,
        Download = Download,
        Upload = Upload,
        Error = Error,
        PacketLoss = PacketLoss,
        BufferbloatDown = BufferbloatDown,
        BufferbloatUp = BufferbloatUp,
        DownloadBytes = DownloadBytes,
        UploadBytes = UploadBytes,
        Status = EnumNames.TryParse<TestStatus>(Status, out var status) ? status : FallbackStatus,
        Healthy = Healthy,
        ThresholdPing = ThresholdPing,
        ThresholdDownload = ThresholdDownload,
        ThresholdUpload = ThresholdUpload,
        Type = EnumNames.TryParse<TestType>(Type, out var type) ? type : TestType.Auto,
        ResultId = ResultId,
        Time = Time,
        Created = Created
    };

    private TestStatus FallbackStatus => string.IsNullOrEmpty(Error) ? TestStatus.Completed : TestStatus.Failed;
}
