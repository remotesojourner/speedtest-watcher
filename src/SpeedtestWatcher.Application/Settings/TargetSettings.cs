using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Settings;

public sealed record TargetSettings(int? Ping, double? Download, double? Upload)
{
    public bool? Evaluate(int ping, double download, double upload) =>
        Ping == null && Download == null && Upload == null ? null : Missed(ping, download, upload).Count == 0;

    public IReadOnlyList<TargetKind> Missed(int ping, double download, double upload)
    {
        var missed = new List<TargetKind>();
        if (Ping is { } maxPing && ping > maxPing) missed.Add(TargetKind.Ping);
        if (Download is { } minDownload && download < minDownload) missed.Add(TargetKind.Download);
        if (Upload is { } minUpload && upload < minUpload) missed.Add(TargetKind.Upload);
        return missed;
    }

    public static string Describe(IReadOnlyList<TargetKind> targets) => targets.Count switch
    {
        0 => "",
        1 => targets[0].ToName(),
        _ => $"{string.Join(", ", targets.SkipLast(1).Select(target => target.ToName()))} and {targets[^1].ToName()}"
    };
}
