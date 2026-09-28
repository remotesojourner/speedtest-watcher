using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SpeedtestWatcher.Application.Extensions;

namespace SpeedtestWatcher.Application.Data.Converters;

internal sealed class EnumNameConverter<TEnum>() : ValueConverter<TEnum, string>(value => value.ToName(), stored => FromName(stored))
    where TEnum : struct, Enum
{
    private static TEnum FromName(string stored) => EnumExtensions.TryParse<TEnum>(stored, out var value) ? value : default;
}
