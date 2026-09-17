using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SpeedtestWatcher.Infrastructure.Data;

public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
        stored => DateTime.SpecifyKind(stored, DateTimeKind.Utc))
    {
    }
}
