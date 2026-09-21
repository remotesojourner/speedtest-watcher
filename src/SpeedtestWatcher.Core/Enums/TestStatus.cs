using System.Text.Json.Serialization;
using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Core.Enums;

[JsonConverter(typeof(CamelCaseEnumConverter<TestStatus>))]
public enum TestStatus
{
    Completed,
    Failed,
    Skipped
}
