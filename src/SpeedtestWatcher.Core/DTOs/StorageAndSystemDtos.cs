using System.Text.Json.Serialization;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Core.DTOs;

public class StorageInfoDto
{
    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("testCount")]
    public int TestCount { get; set; }
}

public class SettingsBackupDto
{
    [JsonPropertyName("config")]
    public List<ConfigEntry> Config { get; set; } = [];

    [JsonPropertyName("integrations")]
    public List<IntegrationData> Integrations { get; set; } = [];

    [JsonPropertyName("recommendations")]
    public Recommendation? Recommendations { get; set; }
}

public class SettingsImportResultDto
{
    [JsonPropertyName("settings")]
    public int Settings { get; set; }

    [JsonPropertyName("integrations")]
    public int Integrations { get; set; }

    [JsonPropertyName("recommendations")]
    public bool Recommendations { get; set; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }
}

public class TestImportResultDto
{
    [JsonPropertyName("imported")]
    public int Imported { get; set; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }
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

