using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Enums;

namespace SpeedtestWatcher.Web.Services;

public sealed class RecentResults
{
    public const int Size = 30;

    private readonly ResultsService _results;
    private readonly List<SpeedtestDto> _tests = [];

    public RecentResults(ResultsService results)
    {
        _results = results;
    }

    public IReadOnlyList<SpeedtestDto> Tests => _tests;

    public SpeedtestDto? Latest => _tests.FirstOrDefault();

    public SpeedtestDto? LatestCompleted => _tests.FirstOrDefault(test => test.Status == TestStatus.Completed);

    public event Action? OnChange;

    public event Action<SpeedtestDto>? ResultAdded;

    public async Task ReloadAsync()
    {
        var latest = await _results.ListAsync(null, Size, null, null, null);
        _tests.Clear();
        _tests.AddRange(latest);
        OnChange?.Invoke();
    }

    public void Add(SpeedtestDto test)
    {
        if (_tests.Any(existing => existing.Id == test.Id)) return;

        _tests.Insert(0, test);
        if (_tests.Count > Size) _tests.RemoveAt(_tests.Count - 1);
        ResultAdded?.Invoke(test);
        OnChange?.Invoke();
    }

    public void Remove(int id)
    {
        if (_tests.RemoveAll(test => test.Id == id) > 0) OnChange?.Invoke();
    }

    public void Clear()
    {
        _tests.Clear();
        OnChange?.Invoke();
    }
}
