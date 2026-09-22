using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Speedtests;

[JsonConverter(typeof(CamelCaseEnumConverter<TestStatus>))]
public enum TestStatus
{
    Completed,
    Failed,
    Skipped
}
