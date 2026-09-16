using System.Text.Json;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Infrastructure.SpeedTest;

namespace SpeedtestWatcher.Tests;

public class OutputParserTests
{
    [Fact]
    public void OutputParser_ParseOokla_ParsesCorrectly()
    {
        string json = """
        {
            "type": "result",
            "timestamp": "2026-09-14T00:00:00Z",
            "ping": { "jitter": 1.45, "latency": 12.3 },
            "download": { "bandwidth": 125000000, "bytes": 125000000, "elapsed": 10000 },
            "upload": { "bandwidth": 62500000, "bytes": 62500000, "elapsed": 10000 },
            "server": { "id": 1234, "name": "Vodafone", "host": "speedtest.vodafone.de" },
            "result": { "id": "abc-123", "url": "https://www.speedtest.net/result/abc-123" }
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var result = OutputParser.Parse(SpeedtestProvider.Ookla, doc.RootElement);

        Assert.True(result.Success);
        Assert.Equal(12, result.Ping);
        Assert.Equal(1.45, result.Jitter);
        Assert.Equal(1000.0, result.Download); // 125000000 / 1250 / 100
        Assert.Equal(500.0, result.Upload);
        Assert.Equal(20, result.Time);
        Assert.Equal(1234, result.ServerId);
        Assert.Equal("Vodafone", result.ServerName);
        Assert.Equal("speedtest.vodafone.de", result.ServerHost);
        Assert.Equal("abc-123", result.ResultId);
    }

    [Fact]
    public void OutputParser_ParseLibre_ParsesCorrectly()
    {
        string json = """
        {
            "ping": 18.4,
            "jitter": "2.35",
            "download": 240.5,
            "upload": 45.2,
            "elapsed": 12000,
            "server": { "id": 5, "name": "Local Libre", "url": "http://speed.local" }
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var result = OutputParser.Parse(SpeedtestProvider.Libre, doc.RootElement);

        Assert.True(result.Success);
        Assert.Equal(18, result.Ping);
        Assert.Equal(2.35, result.Jitter);
        Assert.Equal(240.5, result.Download);
        Assert.Equal(45.2, result.Upload);
        Assert.Equal(12, result.Time);
        Assert.Equal(5, result.ServerId);
        Assert.Equal("Local Libre", result.ServerName);
        Assert.Equal("http://speed.local", result.ServerHost);
    }

    [Fact]
    public void OutputParser_ParseCloudflare_ParsesCorrectly()
    {
        string json = """
        {
            "latency_measurement": {
                "avg_latency_ms": 14.8,
                "latency_measurements": [14.0, 15.2, 14.5, 16.0]
            },
            "speed_measurements": [
                { "test_type": "Download", "max": 450.2, "median": 420.0 },
                { "test_type": "Upload", "max": 95.8, "median": 90.0 }
            ],
            "elapsed": 25000
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var result = OutputParser.Parse(SpeedtestProvider.Cloudflare, doc.RootElement);

        Assert.True(result.Success);
        Assert.Equal(15, result.Ping);
        Assert.NotNull(result.Jitter);
        Assert.Equal(450.2, result.Download);
        Assert.Equal(95.8, result.Upload);
        Assert.Equal(25, result.Time);
    }
}
