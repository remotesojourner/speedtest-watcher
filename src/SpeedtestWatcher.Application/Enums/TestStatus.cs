using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Converters;

namespace SpeedtestWatcher.Application.Enums;

[JsonConverter(typeof(CamelCaseEnumConverter<TestStatus>))]
public enum TestStatus
{
    Completed,
    Failed,
    Skipped
}
