using System.Text.Json;
using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Tests;

public class ConfigDtoTests
{
    [Theory]
    [InlineData("{\"viewMode\":true}", true)]
    [InlineData("{\"viewMode\":false}", false)]
    [InlineData("{\"provider\":\"ookla\"}", false)]
    public void Flags_AreReadFromDeserializedJson(string json, bool expectedViewMode)
    {
        var config = JsonSerializer.Deserialize<ConfigDto>(json)!;

        Assert.Equal(expectedViewMode, config.ViewMode);
    }

    [Fact]
    public void Flags_AreReadFromPlainBooleans()
    {
        var config = new ConfigDto { ["viewMode"] = true };

        Assert.True(config.ViewMode);
    }

    [Fact]
    public void StringValues_AreReadFromDeserializedJson()
    {
        var config = JsonSerializer.Deserialize<ConfigDto>("{\"ping\":\"25\",\"visitorAccess\":\"read\",\"authActive\":true}")!;

        Assert.Equal("25", config.Ping);
        Assert.Equal("read", config.VisitorAccess);
        Assert.True(config.AuthActive);
    }
}
