using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Speedtests;

[JsonConverter(typeof(CamelCaseEnumConverter<TestType>))]
public enum TestType
{
    Auto,
    Custom
}
