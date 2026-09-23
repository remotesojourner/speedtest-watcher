using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.UnitTests.Application.Speedtests;

public class SpeedtestExportCoverageTests
{
    private static readonly PropertyInfo[] _exportedProperties = typeof(Speedtest)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() == null)
        .ToArray();

    [Fact]
    public void EveryExportedFieldHasACsvColumnHoldingItsValue()
    {
        var test = TestWithEveryFieldSet();

        var lines = SpeedtestExport.ToCsv([test]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        var header = lines[0].Split(',');
        var row = lines[1].Split(',');

        Assert.Equal(header.Length, row.Length);
        Assert.Equal(
            _exportedProperties.Select(property => $"{ColumnName(property)}={CellFor(property.GetValue(test))}").Order(),
            header.Zip(row, (column, cell) => $"{column}={cell}").Order());
    }

    [Fact]
    public void EveryExportedFieldIsInTheJsonExport()
    {
        using var json = JsonDocument.Parse(SpeedtestExport.ToJson([TestWithEveryFieldSet()]));

        Assert.Equal(
            _exportedProperties.Select(ColumnName).Order(),
            json.RootElement[0].EnumerateObject().Select(field => field.Name).Order());
    }

    [Fact]
    public void ThePublicIpStaysOutOfBothFormats()
    {
        var test = TestWithEveryFieldSet();
        test.PublicIp = "203.0.113.9";

        Assert.DoesNotContain("203.0.113.9", SpeedtestExport.ToCsv([test]));
        Assert.DoesNotContain("203.0.113.9", SpeedtestExport.ToJson([test]));
    }

    private static Speedtest TestWithEveryFieldSet()
    {
        var test = new Speedtest
        {
            Status = TestStatus.Failed,
            Type = TestType.Custom,
            Healthy = false,
            Created = new DateTime(2026, 9, 16, 8, 5, 0, DateTimeKind.Utc)
        };

        var next = 0;
        foreach (var property in _exportedProperties)
        {
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (type == typeof(int)) property.SetValue(test, ++next);
            else if (type == typeof(long)) property.SetValue(test, ++next * 1_000_000_000L);
            else if (type == typeof(double)) property.SetValue(test, ++next + 0.25);
            else if (type == typeof(string)) property.SetValue(test, $"text{++next}");
        }

        return test;
    }

    private static string ColumnName(PropertyInfo property) => JsonNamingPolicy.CamelCase.ConvertName(property.Name);

    private static string CellFor(object? value) => value switch
    {
        null => "",
        DateTime created => created.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
        TestStatus status => status.ToName(),
        TestType type => type.ToName(),
        bool flag => flag ? "true" : "false",
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };
}
