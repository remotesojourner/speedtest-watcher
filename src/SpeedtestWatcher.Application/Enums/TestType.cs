using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Converters;

namespace SpeedtestWatcher.Application.Enums;

[JsonConverter(typeof(CamelCaseEnumConverter<TestType>))]
public enum TestType
{
    Auto,
    Custom
}
