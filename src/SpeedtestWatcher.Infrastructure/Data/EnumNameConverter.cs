using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SpeedtestWatcher.Core.Helpers;

namespace SpeedtestWatcher.Infrastructure.Data;

public sealed class EnumNameConverter<TEnum>() : ValueConverter<TEnum, string>(value => value.ToName(), stored => FromName(stored))
    where TEnum : struct, Enum
{
    private static TEnum FromName(string stored) => EnumNames.TryParse<TEnum>(stored, out var value) ? value : default;
}
