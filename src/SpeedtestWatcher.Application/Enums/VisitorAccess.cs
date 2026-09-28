using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Converters;

namespace SpeedtestWatcher.Application.Enums;

[JsonConverter(typeof(CamelCaseEnumConverter<VisitorAccess>))]
public enum VisitorAccess
{
    None,
    Read
}
