using System.Globalization;
using System.Text;
using System.Text.Json;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Core.Helpers;

/// <summary>Turns results into the CSV and JSON files the History page downloads.</summary>
public static class SpeedtestExport
{
    public const string CsvHeader =
        "id,created,status,healthy,type,ping,jitter,download,upload,time,serverId,serverName,serverHost,thresholdPing,thresholdDownload,thresholdUpload,resultId,error";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>The same shape the Storage page imports, so an export can be loaded back in.</summary>
    public static string ToJson(IEnumerable<Speedtest> tests) => JsonSerializer.Serialize(tests, JsonOptions);

    public static string ToCsv(IEnumerable<Speedtest> tests)
    {
        var csv = new StringBuilder();
        csv.Append(CsvHeader).Append("\r\n");

        foreach (var test in tests)
        {
            csv.AppendJoin(',',
                    test.Id.ToString(CultureInfo.InvariantCulture),
                    DateTime.SpecifyKind(test.Created, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                    test.Status,
                    test.Healthy switch { true => "true", false => "false", null => "" },
                    test.Type,
                    Number(test.Ping),
                    Number(test.Jitter),
                    Number(test.Download),
                    Number(test.Upload),
                    Number(test.Time),
                    Number(test.ServerId),
                    Text(test.ServerName),
                    Text(test.ServerHost),
                    Number(test.ThresholdPing),
                    Number(test.ThresholdDownload),
                    Number(test.ThresholdUpload),
                    Text(test.ResultId),
                    Text(test.Error))
                .Append("\r\n");
        }

        return csv.ToString();
    }

    private static string Number(double? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";

    // Text comes from provider output, so it is quoted whenever it could break a column, and a leading
    // = + - @ is defused so a spreadsheet opening the file doesn't run it as a formula.
    private static string Text(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r') value = "'" + value;

        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0 || value != value.Trim()
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
