namespace SpeedtestWatcher.Application.Models;

public sealed record ExportFile(byte[] Content, string ContentType, string FileName);
