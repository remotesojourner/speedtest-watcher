using System.Text.Json;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Enums;

namespace SpeedtestWatcher.Tests;

public class ConfigDtoTests
{
    [Theory]
    [InlineData("{\"viewMode\":true}", true)]
    [InlineData("{\"viewMode\":false}", false)]
    [InlineData("{\"provider\":\"ookla\"}", false)]
    public void Flags_AreReadFromDeserializedJson(string json, bool expectedReadOnly)
    {
        var config = JsonSerializer.Deserialize<ConfigDto>(json)!;

        Assert.Equal(expectedReadOnly, config.ReadOnlyVisitor);
    }

    [Fact]
    public void Flags_AreReadFromPlainBooleans()
    {
        var config = new ConfigDto { ["viewMode"] = true };

        Assert.True(config.ReadOnlyVisitor);
    }

    [Fact]
    public void StringValues_AreReadFromDeserializedJson()
    {
        var config = JsonSerializer.Deserialize<ConfigDto>("{\"ping\":\"25\",\"visitorAccess\":\"read\",\"authActive\":true}")!;

        Assert.Equal("25", config.Ping);
        Assert.Equal(VisitorAccess.Read, config.VisitorAccess);
        Assert.True(config.AuthActive);
    }
}
