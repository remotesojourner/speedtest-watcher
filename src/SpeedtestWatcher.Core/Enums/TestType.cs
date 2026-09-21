using System.Text.Json.Serialization;
using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Core.Enums;

[JsonConverter(typeof(CamelCaseEnumConverter<TestType>))]
public enum TestType
{
    Auto,
    Custom
}
