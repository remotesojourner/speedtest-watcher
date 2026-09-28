namespace SpeedtestWatcher.Application.Models;

public sealed record RunOptions(string? ServerId, string? CustomServerUrl, string? NetworkInterface, string ScratchFilePath);
