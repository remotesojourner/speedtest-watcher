using SpeedtestWatcher.Application.Models.Dtos;
using SpeedtestWatcher.Web.Utils;

namespace SpeedtestWatcher.UnitTests.Web.Utils;

public sealed class UpdateCheckTests
{
    [Theory]
    [InlineData("1.4.0", "1.4.1", "1.4.1")]
    [InlineData("1.4.0", "2.0", "2.0")]
    [InlineData("1.4.0", "1.4.0", null)]
    [InlineData("1.4.0", "1.4", null)]
    [InlineData("1.4", "1.4.0", null)]
    [InlineData("1.5.0", "1.4.9", null)]
    [InlineData("1.4.0", "0", null)]
    [InlineData("1.4.0", "not-a-version", null)]
    public void AnUpdateIsOnlyOfferedForANewerRelease(string local, string remote, string? expected)
    {
        Assert.Equal(expected, UpdateCheck.AvailableUpdate(new VersionInfoDto { Local = local, Remote = remote }));
    }

    [Fact]
    public void NoUpdateIsOfferedWhenTheCheckFailed()
    {
        Assert.Null(UpdateCheck.AvailableUpdate(null));
    }
}
