using System.Text.Json.Serialization;
using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Core.Enums;

[JsonConverter(typeof(CamelCaseEnumConverter<ServerMode>))]
public enum ServerMode
{
    Auto,
    Random,
    Single
}
