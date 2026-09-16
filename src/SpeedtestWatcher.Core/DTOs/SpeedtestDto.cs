using System.Text.Json.Serialization;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Core.DTOs;

public class SpeedtestDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("serverId")]
    public int ServerId { get; set; }

    [JsonPropertyName("serverName")]
    public string? ServerName { get; set; }

    [JsonPropertyName("serverHost")]
    public string? ServerHost { get; set; }

    [JsonPropertyName("ping")]
    public int Ping { get; set; }

    [JsonPropertyName("jitter")]
    public double? Jitter { get; set; }

    [JsonPropertyName("download")]
    public double Download { get; set; }

    [JsonPropertyName("upload")]
    public double Upload { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "completed";

    [JsonPropertyName("healthy")]
    public bool? Healthy { get; set; }

    [JsonPropertyName("thresholdPing")]
    public int? ThresholdPing { get; set; }

    [JsonPropertyName("thresholdDownload")]
    public double? ThresholdDownload { get; set; }

    [JsonPropertyName("thresholdUpload")]
    public double? ThresholdUpload { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "auto";

    [JsonPropertyName("resultId")]
    public string? ResultId { get; set; }

    [JsonPropertyName("time")]
    public int Time { get; set; }

    [JsonPropertyName("created")]
    public string Created { get; set; } = string.Empty;

    /// <summary>One place to build the API shape, so a new column can't be missed by one caller.</summary>
    public static SpeedtestDto From(Speedtest test) => new()
    {
        Id = test.Id,
        ServerId = test.ServerId,
        ServerName = test.ServerName,
        ServerHost = test.ServerHost,
        Ping = test.Ping,
        Jitter = test.Jitter,
        Download = test.Download,
        Upload = test.Upload,
        Error = test.Error,
        Status = test.Status,
        Healthy = test.Healthy,
        ThresholdPing = test.ThresholdPing,
        ThresholdDownload = test.ThresholdDownload,
        ThresholdUpload = test.ThresholdUpload,
        Type = test.Type,
        ResultId = test.ResultId,
        Time = test.Time,
        Created = test.Created.ToString("o")
    };
}
