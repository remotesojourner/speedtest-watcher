using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Converters;

namespace SpeedtestWatcher.Application.Enums;

[JsonConverter(typeof(CamelCaseEnumConverter<ServerMode>))]
public enum ServerMode
{
    Auto,
    Random,

    [JsonStringEnumMemberName("single")]
    Pinned
}
