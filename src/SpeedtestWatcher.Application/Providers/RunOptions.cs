namespace SpeedtestWatcher.Application.Providers;

public sealed record RunOptions(string? ServerId, string? CustomServerUrl, string? NetworkInterface, string ScratchFilePath);
