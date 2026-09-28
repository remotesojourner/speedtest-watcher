using System.Text.Json;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Models.Dtos;

namespace SpeedtestWatcher.Application.Utils;

public static class DataBackupFile
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public static DataBackupDto? Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind switch
            {
                JsonValueKind.Array => new DataBackupDto { Speedtests = JsonSerializer.Deserialize<List<SpeedtestImportRow>>(json, _jsonOptions) ?? [] },
                JsonValueKind.Object => JsonSerializer.Deserialize<DataBackupDto>(json, _jsonOptions),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
