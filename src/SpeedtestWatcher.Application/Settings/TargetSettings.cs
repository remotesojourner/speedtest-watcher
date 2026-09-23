using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Application.Settings;

public readonly record struct Readings(int Ping, double Download, double Upload, double? PacketLoss = null, double? Bufferbloat = null);

public sealed record TargetSettings(int? Ping, double? Download, double? Upload, double? PacketLoss = null, double? Bufferbloat = null)
{
    public bool Any => Ping != null || Download != null || Upload != null || PacketLoss != null || Bufferbloat != null;

    public bool? Evaluate(Readings readings) => Any ? Missed(readings).Count == 0 : null;

    public IReadOnlyList<TargetKind> Missed(Readings readings)
    {
        var missed = new List<TargetKind>();
        if (Ping is { } maxPing && readings.Ping > maxPing) missed.Add(TargetKind.Ping);
        if (Download is { } minDownload && readings.Download < minDownload) missed.Add(TargetKind.Download);
        if (Upload is { } minUpload && readings.Upload < minUpload) missed.Add(TargetKind.Upload);
        if (PacketLoss is { } maxLoss && readings.PacketLoss is { } loss && loss > maxLoss) missed.Add(TargetKind.PacketLoss);
        if (Bufferbloat is { } maxBloat && readings.Bufferbloat is { } bloat && bloat > maxBloat) missed.Add(TargetKind.Bufferbloat);
        return missed;
    }

    public static string Describe(IReadOnlyList<TargetKind> targets) => targets.Count switch
    {
        0 => "",
        1 => Wording(targets[0]),
        _ => $"{string.Join(", ", targets.SkipLast(1).Select(Wording))} and {Wording(targets[^1])}"
    };

    private static string Wording(TargetKind kind) => kind switch
    {
        TargetKind.PacketLoss => "packet loss",
        _ => kind.ToName()
    };
}
