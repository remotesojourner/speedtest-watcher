namespace SpeedtestWatcher.Application.Settings;

public sealed record TargetSettings(int? Ping, double? Download, double? Upload)
{
    public bool? Evaluate(int ping, double download, double upload)
    {
        if (Ping == null && Download == null && Upload == null) return null;
        if (Ping is { } maxPing && ping > maxPing) return false;
        if (Download is { } minDownload && download < minDownload) return false;
        if (Upload is { } minUpload && upload < minUpload) return false;
        return true;
    }
}
