using System.Text.Json;
using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Tests;

public class ConfigDtoTests
{
    [Theory]
    [InlineData("{\"viewMode\":true,\"previewMode\":true}", true, true)]
    [InlineData("{\"viewMode\":false,\"previewMode\":false}", false, false)]
    [InlineData("{\"provider\":\"ookla\"}", false, false)]
    public void Flags_AreReadFromDeserializedJson(string json, bool expectedViewMode, bool expectedPreviewMode)
    {
        var config = JsonSerializer.Deserialize<ConfigDto>(json)!;

        Assert.Equal(expectedViewMode, config.ViewMode);
        Assert.Equal(expectedPreviewMode, config.PreviewMode);
    }

    [Fact]
    public void Flags_AreReadFromPlainBooleans()
    {
        var config = new ConfigDto { ["viewMode"] = true, ["previewMode"] = false };

        Assert.True(config.ViewMode);
        Assert.False(config.PreviewMode);
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
