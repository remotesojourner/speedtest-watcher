using System.Text.Json.Serialization;
using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Core.Enums;

[JsonConverter(typeof(CamelCaseEnumConverter<VisitorAccess>))]
public enum VisitorAccess
{
    None,
    Read
}
