using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Providers;

[JsonConverter(typeof(CamelCaseEnumConverter<ServerMode>))]
public enum ServerMode
{
    Auto,
    Random,

    [JsonStringEnumMemberName("single")]
    Pinned
}
