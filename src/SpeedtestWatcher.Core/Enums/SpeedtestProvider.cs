using System.Text.Json.Serialization;
using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Core.Enums;

[JsonConverter(typeof(CamelCaseEnumConverter<SpeedtestProvider>))]
public enum SpeedtestProvider
{
    None,
    Ookla,
    Libre,
    Cloudflare
}
