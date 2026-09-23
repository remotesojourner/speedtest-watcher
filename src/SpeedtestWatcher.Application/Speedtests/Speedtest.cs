using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Speedtests;

public class Speedtest
{
    public int Id { get; set; }
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

    [JsonIgnore]
    public string? PublicIp { get; set; }

    public TestStatus Status { get; set; } = TestStatus.Completed;

    public bool? Healthy { get; set; }

    public int? ThresholdPing { get; set; }
    public double? ThresholdDownload { get; set; }
    public double? ThresholdUpload { get; set; }

    public double? ThresholdPacketLoss { get; set; }

    public double? ThresholdBufferbloat { get; set; }

    public TestType Type { get; set; } = TestType.Auto;
    public string? ResultId { get; set; }
    public int Time { get; set; }
    public DateTime Created { get; set => field = TimeZones.AsUtc(value); } = DateTime.UtcNow;
}
