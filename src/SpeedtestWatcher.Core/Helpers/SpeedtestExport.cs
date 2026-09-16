using System.Globalization;
using System.Text;
using System.Text.Json;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Core.Helpers;

public static class SpeedtestExport
{
    public const string CsvHeader =
        "id,created,status,healthy,type,ping,jitter,download,upload,time,serverId,serverName,serverHost,thresholdPing,thresholdDownload,thresholdUpload,resultId,error";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

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

    private static string Text(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r') value = "'" + value;

        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0 || value != value.Trim()
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
