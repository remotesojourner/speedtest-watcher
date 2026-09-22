namespace SpeedtestWatcher.Core.DTOs;

public class ConfigDto : Dictionary<string, object?>;

public class UpdateConfigKeyRequest
{
    public object? Value { get; set; }
}
