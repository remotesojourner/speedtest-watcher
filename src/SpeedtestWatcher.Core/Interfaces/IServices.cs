using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Enums;

namespace SpeedtestWatcher.Core.Interfaces;

public class SpeedtestExecutionResult
{
    public bool Success { get; set; }

    public bool Skipped { get; set; }
    public int Ping { get; set; }
    public double? Jitter { get; set; }
    public double Download { get; set; }
    public double Upload { get; set; }
    public int Time { get; set; }
    public int ServerId { get; set; }
    public string? ServerName { get; set; }
    public string? ServerHost { get; set; }
    public string? ResultId { get; set; }
    public string? Error { get; set; }
}

public interface ISpeedtestRunner
{
    Task<SpeedtestExecutionResult> RunTestAsync(SpeedtestProvider provider, string? serverId, string? customUrl, string? networkInterface, CancellationToken cancellationToken = default);
}

public interface IIntegrationDispatcher
{
    Task TriggerEventAsync(IntegrationEvent eventType, object? eventData, CancellationToken cancellationToken = default);
    Dictionary<string, IntegrationTypeSchemaDto> GetRegisteredIntegrationSchemas();
}

public interface ICliManager
{
    Task EnsureBinariesAsync(CancellationToken cancellationToken = default);
    string GetBinaryPath(SpeedtestProvider provider);
    bool IsBinaryAvailable(SpeedtestProvider provider);
}

public interface INetworkInterfaceDetector
{
    Task<Dictionary<string, List<string>>> GetInterfacesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}

public interface IPauseStateService
{
    bool IsPaused { get; }
    bool IsRunning { get; }
    DateTime? ResumesAt { get; }
    void SetRunning(bool running);
    void Pause(double? hours);
    void Resume();
    event Action? OnStatusChanged;
}
