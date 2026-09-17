using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Web.Services;

public class SpeedtestStateService
{
    public const int PageSize = 30;

    private readonly ApiClient _api;

    public SpeedtestStateService(ApiClient api)
    {
        _api = api;
    }

    public List<SpeedtestDto> Tests { get; private set; } = [];
    public bool Loading { get; private set; }
    public bool LoadFailed { get; private set; }
    public bool HasMore { get; private set; } = true;
    public int? LastId => Tests.Count > 0 ? Tests.Last().Id : null;
    public SpeedtestDto? LatestTest => Tests.FirstOrDefault();
    public event Action? OnChange;

    public async Task<bool> ReloadAsync()
    {
        SetLoading(true);
        var result = await _api.GetAsync<List<SpeedtestDto>>($"/api/speedtests?limit={PageSize}");
        if (result.Value != null) SetTests(result.Value, result.Value.Count >= PageSize);
        else SetLoadFailed();
        SetLoading(false);
        return result.Value != null;
    }

    public void SetTests(List<SpeedtestDto> tests, bool hasMore = true)
    {
        Tests = tests;
        HasMore = hasMore;
        LoadFailed = false;
        OnChange?.Invoke();
    }

    public void SetLoadFailed()
    {
        LoadFailed = true;
        OnChange?.Invoke();
    }

    public void PrependTest(SpeedtestDto test)
    {
        if (!Tests.Any(t => t.Id == test.Id))
        {
            Tests.Insert(0, test);
            OnChange?.Invoke();
        }
    }

    public void RemoveTest(int id)
    {
        Tests.RemoveAll(t => t.Id == id);
        OnChange?.Invoke();
    }

    public void SetLoading(bool loading)
    {
        Loading = loading;
        OnChange?.Invoke();
    }
}
