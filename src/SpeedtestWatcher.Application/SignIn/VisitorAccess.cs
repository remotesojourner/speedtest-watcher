using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.SignIn;

[JsonConverter(typeof(CamelCaseEnumConverter<VisitorAccess>))]
public enum VisitorAccess
{
    None,
    Read
}
