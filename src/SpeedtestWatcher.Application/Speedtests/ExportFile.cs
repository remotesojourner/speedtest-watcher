namespace SpeedtestWatcher.Application.Speedtests;

public sealed record ExportFile(byte[] Content, string ContentType, string FileName);
