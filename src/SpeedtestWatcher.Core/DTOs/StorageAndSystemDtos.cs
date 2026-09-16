using System.Text.Json.Serialization;

namespace SpeedtestWatcher.Core.DTOs;

public class StorageInfoDto
{
    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("testCount")]
    public int TestCount { get; set; }
}

public class VersionInfoDto
{
    [JsonPropertyName("local")]
    public string Local { get; set; } = "1.0.9";

    [JsonPropertyName("remote")]
    public string Remote { get; set; } = "0";
}

public class StatusDto
{
    [JsonPropertyName("paused")]
    public bool Paused { get; set; }

    [JsonPropertyName("running")]
    public bool Running { get; set; }
}

public class PauseRequest
{
    [JsonPropertyName("resumeIn")]
    public double? ResumeIn { get; set; }
}

