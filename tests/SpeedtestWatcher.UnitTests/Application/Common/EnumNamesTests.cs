using System.Text.Json;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.UnitTests.Application.Common;

public class EnumNamesTests
{
    [Fact]
    public void NamesAreTheCamelCaseMemberNames()
    {
        Assert.Equal("completed", TestStatus.Completed.ToName());
        Assert.True(EnumNames.TryParse<TestStatus>("Skipped", out var status));
        Assert.Equal(TestStatus.Skipped, status);
    }

    [Fact]
    public void APinnedServerIsStillStoredAndSentAsSingle()
    {
        Assert.Equal("single", ServerMode.Pinned.ToName());
        Assert.True(EnumNames.TryParse<ServerMode>("single", out var mode));
        Assert.Equal(ServerMode.Pinned, mode);
        Assert.Equal("\"single\"", JsonSerializer.Serialize(ServerMode.Pinned));
        Assert.Equal(ServerMode.Pinned, JsonSerializer.Deserialize<ServerMode>("\"single\""));
    }
}
