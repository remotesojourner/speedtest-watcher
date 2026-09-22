namespace SpeedtestWatcher.Core.SpeedTest;

public sealed record RunOptions(string? ServerId, string? CustomServerUrl, string? NetworkInterface, string ScratchFilePath);
