using System.Text.Json;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Extensions;

namespace SpeedtestWatcher.UnitTests.Application.Extensions;

public class EnumExtensionsTests
{
    [Fact]
    public void NamesAreTheCamelCaseMemberNames()
    {
        Assert.Equal("completed", TestStatus.Completed.ToName());
        Assert.True(EnumExtensions.TryParse<TestStatus>("Skipped", out var status));
        Assert.Equal(TestStatus.Skipped, status);
    }

    [Fact]
    public void APinnedServerIsStillStoredAndSentAsSingle()
    {
        Assert.Equal("single", ServerMode.Pinned.ToName());
        Assert.True(EnumExtensions.TryParse<ServerMode>("single", out var mode));
        Assert.Equal(ServerMode.Pinned, mode);
        Assert.Equal("\"single\"", JsonSerializer.Serialize(ServerMode.Pinned));
        Assert.Equal(ServerMode.Pinned, JsonSerializer.Deserialize<ServerMode>("\"single\""));
    }
}
