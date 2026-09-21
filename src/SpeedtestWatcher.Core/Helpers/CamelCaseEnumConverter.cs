using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeedtestWatcher.Core.Helpers;

public sealed class CamelCaseEnumConverter<TEnum>() : JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    where TEnum : struct, Enum;
