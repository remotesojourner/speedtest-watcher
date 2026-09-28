using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Converters;

namespace SpeedtestWatcher.Application.Enums;

[JsonConverter(typeof(CamelCaseEnumConverter<ServerListMode>))]
public enum ServerListMode
{
    Allow,
    Deny
}
