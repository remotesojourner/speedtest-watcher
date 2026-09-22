using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.UnitTests.Application.Common;

public class ByteSizeTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(991777425, "945.8 MB")]
    [InlineData(773408890000, "720.3 GB")]
    [InlineData(1099511627776, "1.0 TB")]
    public void SizesReadInTheLargestUnitThatFits(long bytes, string expected)
    {
        Assert.Equal(expected, ByteSize.Describe(bytes));
    }
}
