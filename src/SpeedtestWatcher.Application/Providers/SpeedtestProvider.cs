using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Providers;

[JsonConverter(typeof(CamelCaseEnumConverter<SpeedtestProvider>))]
public enum SpeedtestProvider
{
    None,
    Ookla,
    Libre,
    Cloudflare
}
