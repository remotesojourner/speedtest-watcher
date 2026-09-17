using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Web.Services;

public class ConfigStateService
{
    private readonly ApiClient _api;

    public ConfigStateService(ApiClient api)
    {
        _api = api;
    }

    public ConfigDto CurrentConfig { get; private set; } = new();
    public bool Loaded { get; private set; }
    public event Action? OnChange;

    public void SetConfig(ConfigDto config)
    {
        CurrentConfig = config;
        Loaded = true;
        OnChange?.Invoke();
    }

    public async Task<ApiResult> SaveAsync(params (string Key, string Value)[] changes)
    {
        foreach (var (key, value) in changes)
        {
            var result = await _api.PatchAsync($"/api/config/{key}", new UpdateConfigKeyRequest { Value = value });
            if (!result.Succeeded) return result;

            CurrentConfig[key] = value;
        }

        return ApiResult.Ok;
    }

    public async Task<bool> ReloadAsync()
    {
        var result = await _api.GetAsync<ConfigDto>("/api/config");
        if (result.Value == null) return false;

        SetConfig(result.Value);
        return true;
    }
}
