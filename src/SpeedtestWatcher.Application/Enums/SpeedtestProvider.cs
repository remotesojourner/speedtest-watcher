using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Converters;

namespace SpeedtestWatcher.Application.Enums;

[JsonConverter(typeof(CamelCaseEnumConverter<SpeedtestProvider>))]
public enum SpeedtestProvider
{
    None,
    Ookla,
    Libre,
    Cloudflare,
    Iperf3
}
