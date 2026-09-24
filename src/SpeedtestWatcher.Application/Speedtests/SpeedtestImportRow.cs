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

    public double? LatencyIdle { get; set; }

    public double? LatencyLoaded { get; set; }

    public double? LatencyLoadedTail { get; set; }

    public long? DownloadBytes { get; set; }
    public long? UploadBytes { get; set; }
    public string? PublicIp { get; set; }
    public string? Status { get; set; }
    public bool? Healthy { get; set; }
    public int? ThresholdPing { get; set; }
    public double? ThresholdDownload { get; set; }
    public double? ThresholdUpload { get; set; }

    public double? ThresholdPacketLoss { get; set; }

    public double? ThresholdBufferbloat { get; set; }

    public string? Type { get; set; }
    public string? ResultId { get; set; }
    public int Time { get; set; }
    public DateTime? Created { get; set; }

    public static SpeedtestImportRow From(Speedtest test) => new()
    {
        ServerId = test.ServerId,
        ServerName = test.ServerName,
        ServerHost = test.ServerHost,
        Ping = test.Ping,
        Jitter = test.Jitter,
        Download = test.Download,
        Upload = test.Upload,
        Error = test.Error,
        PacketLoss = test.PacketLoss,
        BufferbloatDown = test.BufferbloatDown,
        BufferbloatUp = test.BufferbloatUp,
        LatencyIdle = test.LatencyIdle,
        LatencyLoaded = test.LatencyLoaded,
        LatencyLoadedTail = test.LatencyLoadedTail,
        DownloadBytes = test.DownloadBytes,
        UploadBytes = test.UploadBytes,
        PublicIp = test.PublicIp,
        Status = test.Status.ToName(),
        Healthy = test.Healthy,
        ThresholdPing = test.ThresholdPing,
        ThresholdDownload = test.ThresholdDownload,
        ThresholdUpload = test.ThresholdUpload,
        ThresholdPacketLoss = test.ThresholdPacketLoss,
        ThresholdBufferbloat = test.ThresholdBufferbloat,
        Type = test.Type.ToName(),
        ResultId = test.ResultId,
        Time = test.Time,
        Created = test.Created
    };

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
        LatencyIdle = LatencyIdle,
        LatencyLoaded = LatencyLoaded,
        LatencyLoadedTail = LatencyLoadedTail,
        DownloadBytes = DownloadBytes,
        UploadBytes = UploadBytes,
        PublicIp = PublicIp,
        Status = EnumNames.TryParse<TestStatus>(Status, out var status) ? status : FallbackStatus,
        Healthy = Healthy,
        ThresholdPing = ThresholdPing,
        ThresholdDownload = ThresholdDownload,
        ThresholdUpload = ThresholdUpload,
        ThresholdPacketLoss = ThresholdPacketLoss,
        ThresholdBufferbloat = ThresholdBufferbloat,
        Type = EnumNames.TryParse<TestType>(Type, out var type) ? type : TestType.Auto,
        ResultId = ResultId,
        Time = Time,
        Created = Created!.Value
    };

    private TestStatus FallbackStatus => string.IsNullOrEmpty(Error) ? TestStatus.Completed : TestStatus.Failed;
}
