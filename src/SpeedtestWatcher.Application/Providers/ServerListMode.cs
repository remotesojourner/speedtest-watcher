using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Providers;

[JsonConverter(typeof(CamelCaseEnumConverter<ServerListMode>))]
public enum ServerListMode
{
    Allow,
    Deny
}
