using System.Data.Common;
using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Web.Services;

public sealed class PagedTestList
{
    public const int PageSize = 30;

    private readonly Func<TestFilter, int?, int, Task<IReadOnlyList<SpeedtestDto>>> _loadPage;
    private readonly List<SpeedtestDto> _items = [];
    private int _generation;

    public PagedTestList(Func<TestFilter, int?, int, Task<IReadOnlyList<SpeedtestDto>>> loadPage)
    {
        _loadPage = loadPage;
    }

    public TestFilter Filter { get; private set; } = TestFilter.None;

    public IReadOnlyList<SpeedtestDto> Items => _items;

    public bool HasMore { get; private set; } = true;

    public bool Loading { get; private set; }

    public bool LoadFailed { get; private set; }

    public bool LoadMoreFailed { get; private set; }

    public Task ReloadAsync(TestFilter filter)
    {
        Filter = filter;
        _items.Clear();
        HasMore = true;
        LoadFailed = false;
        LoadMoreFailed = false;
        return LoadPageAsync(firstPage: true);
    }

    public Task LoadMoreAsync() => HasMore && !Loading ? LoadPageAsync(firstPage: false) : Task.CompletedTask;

    public bool Prepend(SpeedtestDto test)
    {
        if (!Filter.Matches(test) || _items.Any(item => item.Id == test.Id)) return false;

        _items.Insert(0, test);
        return true;
    }

    public void Remove(int id) => _items.RemoveAll(item => item.Id == id);

    private async Task LoadPageAsync(bool firstPage)
    {
        var generation = ++_generation;
        Loading = true;
        LoadMoreFailed = false;
        try
        {
            var page = await _loadPage(Filter, _items.LastOrDefault()?.Id, PageSize);
            if (generation != _generation) return;

            _items.AddRange(page);
            HasMore = page.Count >= PageSize;
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException)
        {
            if (generation != _generation) return;

            if (firstPage) LoadFailed = true;
            else LoadMoreFailed = true;
        }
        finally
        {
            if (generation == _generation) Loading = false;
        }
    }
}
