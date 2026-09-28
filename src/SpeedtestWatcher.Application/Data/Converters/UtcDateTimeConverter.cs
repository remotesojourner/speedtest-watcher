using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SpeedtestWatcher.Application.Data.Converters;

internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
        stored => DateTime.SpecifyKind(stored, DateTimeKind.Utc))
    {
    }
}
