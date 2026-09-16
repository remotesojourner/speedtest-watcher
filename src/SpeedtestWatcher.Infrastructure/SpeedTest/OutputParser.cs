using System.Text.Json;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Infrastructure.SpeedTest;

public static class OutputParser
{
    private static double RoundSpeed(double bandwidth)
    {
        return Math.Round(bandwidth / 1250.0, 2) / 100.0;
    }

    private static double? CalculateJitter(IList<double> latencyMeasurements)
    {
        if (latencyMeasurements == null || latencyMeasurements.Count < 2) return null;
        double totalDiff = 0;
        for (int i = 1; i < latencyMeasurements.Count; i++)
        {
            totalDiff += Math.Abs(latencyMeasurements[i] - latencyMeasurements[i - 1]);
        }
        return Math.Round(totalDiff / (latencyMeasurements.Count - 1), 2);
    }

    public static SpeedtestExecutionResult Parse(SpeedtestProvider provider, JsonElement json)
    {
        return provider switch
        {
            SpeedtestProvider.Ookla => ParseOokla(json),
            SpeedtestProvider.Libre => ParseLibre(json),
            SpeedtestProvider.Cloudflare => ParseCloudflare(json),
            _ => throw new ArgumentException("Invalid provider", nameof(provider))
        };
    }

    public static SpeedtestExecutionResult ParseOokla(JsonElement root)
    {
        var result = new SpeedtestExecutionResult { Success = true };

        if (root.TryGetProperty("ping", out var pingElem))
        {
            if (pingElem.TryGetProperty("latency", out var latElem))
                result.Ping = (int)Math.Round(latElem.GetDouble());
            if (pingElem.TryGetProperty("jitter", out var jitElem))
                result.Jitter = Math.Round(jitElem.GetDouble(), 2);
        }

        double downElapsed = 0, upElapsed = 0;
        if (root.TryGetProperty("download", out var downElem))
        {
            if (downElem.TryGetProperty("bandwidth", out var bwElem))
                result.Download = RoundSpeed(bwElem.GetDouble());
            if (downElem.TryGetProperty("elapsed", out var elElem))
                downElapsed = elElem.GetDouble();
        }

        if (root.TryGetProperty("upload", out var upElem))
        {
            if (upElem.TryGetProperty("bandwidth", out var ubwElem))
                result.Upload = RoundSpeed(ubwElem.GetDouble());
            if (upElem.TryGetProperty("elapsed", out var uelElem))
                upElapsed = uelElem.GetDouble();
        }

        result.Time = (int)Math.Round((downElapsed + upElapsed) / 1000.0);

        if (root.TryGetProperty("server", out var serverElem))
        {
            if (serverElem.TryGetProperty("id", out var sidElem))
                result.ServerId = sidElem.GetInt32();
            if (serverElem.TryGetProperty("name", out var snameElem))
                result.ServerName = snameElem.GetString();
            if (serverElem.TryGetProperty("host", out var shostElem))
                result.ServerHost = shostElem.GetString();
        }

        if (root.TryGetProperty("result", out var resElem) && resElem.TryGetProperty("id", out var ridElem))
        {
            result.ResultId = ridElem.GetString();
        }

        return result;
    }

    public static SpeedtestExecutionResult ParseLibre(JsonElement root)
    {
        var result = new SpeedtestExecutionResult { Success = true };

        if (root.TryGetProperty("ping", out var pElem))
            result.Ping = (int)Math.Round(pElem.GetDouble());

        if (root.TryGetProperty("jitter", out var jElem))
        {
            if (jElem.ValueKind == JsonValueKind.Number)
                result.Jitter = Math.Round(jElem.GetDouble(), 2);
            else if (jElem.ValueKind == JsonValueKind.String && double.TryParse(jElem.GetString(), out var jd))
                result.Jitter = Math.Round(jd, 2);
        }

        if (root.TryGetProperty("download", out var dElem))
            result.Download = Math.Round(dElem.GetDouble(), 2);

        if (root.TryGetProperty("upload", out var uElem))
            result.Upload = Math.Round(uElem.GetDouble(), 2);

        if (root.TryGetProperty("elapsed", out var eElem))
            result.Time = (int)Math.Round(eElem.GetDouble() / 1000.0);

        if (root.TryGetProperty("server", out var sElem))
        {
            if (sElem.TryGetProperty("id", out var sidElem))
                result.ServerId = sidElem.GetInt32();
            if (sElem.TryGetProperty("name", out var snElem))
                result.ServerName = snElem.GetString();
            if (sElem.TryGetProperty("url", out var suElem))
                result.ServerHost = suElem.GetString();
        }

        return result;
    }

    public static SpeedtestExecutionResult ParseCloudflare(JsonElement root)
    {
        var result = new SpeedtestExecutionResult { Success = true };

        if (root.TryGetProperty("speed_measurements", out var smElem) && smElem.ValueKind == JsonValueKind.Array)
        {
            var downSpeeds = new List<double>();
            var upSpeeds = new List<double>();

            foreach (var item in smElem.EnumerateArray())
            {
                string? type = item.TryGetProperty("test_type", out var tt) ? tt.GetString() : null;
                double speed = 0;
                if (item.TryGetProperty("max", out var mx)) speed = mx.GetDouble();
                else if (item.TryGetProperty("median", out var med)) speed = med.GetDouble();

                if (type == "Download") downSpeeds.Add(speed);
                else if (type == "Upload") upSpeeds.Add(speed);
            }

            result.Download = downSpeeds.Count > 0 ? Math.Round(downSpeeds.Max(), 2) : 0;
            result.Upload = upSpeeds.Count > 0 ? Math.Round(upSpeeds.Max(), 2) : 0;
        }

        if (root.TryGetProperty("latency_measurement", out var lmElem))
        {
            if (lmElem.TryGetProperty("avg_latency_ms", out var avgLat))
                result.Ping = (int)Math.Round(avgLat.GetDouble());

            if (lmElem.TryGetProperty("latency_measurements", out var latArr) && latArr.ValueKind == JsonValueKind.Array)
            {
                var lats = latArr.EnumerateArray().Select(x => x.GetDouble()).ToList();
                result.Jitter = CalculateJitter(lats);
            }
        }

        if (root.TryGetProperty("elapsed", out var elElem))
            result.Time = (int)Math.Round(elElem.GetDouble() / 1000.0);
        else
            result.Time = 30;

        return result;
    }
}
