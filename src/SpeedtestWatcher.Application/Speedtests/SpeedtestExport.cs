using System.Globalization;
using System.Text;
using System.Text.Json;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Speedtests;

public static class SpeedtestExport
{
    public const string CsvHeader =
        "id,created,status,healthy,type,ping,jitter,download,upload,packetLoss,bufferbloatDown,bufferbloatUp,downloadBytes,uploadBytes,time,serverId,serverName,serverHost,thresholdPing,thresholdDownload,thresholdUpload,resultId,error";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static string ToJson(IEnumerable<Speedtest> tests) => JsonSerializer.Serialize(tests, _jsonOptions);

    public static string ToCsv(IEnumerable<Speedtest> tests)
    {
        var csv = new StringBuilder();
        csv.Append(CsvHeader).Append("\r\n");

        foreach (var test in tests)
        {
            csv.AppendJoin(',',
                    test.Id.ToString(CultureInfo.InvariantCulture),
                    test.Created.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                    test.Status.ToName(),
                    test.Healthy switch { true => "true", false => "false", null => "" },
                    test.Type.ToName(),
                    Number(test.Ping),
                    Number(test.Jitter),
                    Number(test.Download),
                    Number(test.Upload),
                    Number(test.PacketLoss),
                    Number(test.BufferbloatDown),
                    Number(test.BufferbloatUp),
                    Number(test.DownloadBytes),
                    Number(test.UploadBytes),
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
