using Microsoft.Data.Sqlite;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Web.Services;

namespace SpeedtestWatcher.Tests;

public class PagedTestListTests
{
    private readonly List<(TestFilter Filter, int? AfterId, int Limit)> _requests = [];

    [Fact]
    public async Task TheFirstPage_IsLoadedWithTheFilter()
    {
        var filter = new TestFilter(Status: TestStatus.Failed);
        var list = new PagedTestList(Pages(Page(100, PagedTestList.PageSize)));

        await list.ReloadAsync(filter);

        Assert.Equal((filter, (int?)null, PagedTestList.PageSize), Assert.Single(_requests));
        Assert.Equal(PagedTestList.PageSize, list.Items.Count);
        Assert.True(list.HasMore);
        Assert.False(list.Loading);
    }

    [Fact]
    public async Task LoadingMore_ContinuesAfterTheLastTest_AndStopsAtAShortPage()
    {
        var list = new PagedTestList(Pages(Page(100, PagedTestList.PageSize), Page(70, 5)));
        await list.ReloadAsync(TestFilter.None);

        await list.LoadMoreAsync();
        await list.LoadMoreAsync();

        Assert.Equal(71, _requests[1].AfterId);
        Assert.Equal(2, _requests.Count);
        Assert.Equal(PagedTestList.PageSize + 5, list.Items.Count);
        Assert.False(list.HasMore);
    }

    [Fact]
    public async Task ChangingTheFilter_DiscardsAPageStillLoadingForTheOldOne()
    {
        var slow = new TaskCompletionSource<IReadOnlyList<SpeedtestDto>>();
        var list = new PagedTestList((filter, _, _) => filter.IsActive ? Task.FromResult(Page(5, 2)) : slow.Task);

        var stale = list.ReloadAsync(TestFilter.None);
        await list.ReloadAsync(new TestFilter(Healthy: false));
        slow.SetResult(Page(100, 3));
        await stale;

        Assert.Equal([5, 4], list.Items.Select(test => test.Id));
        Assert.False(list.Loading);
    }

    [Fact]
    public async Task NewResults_AreAddedOnlyWhenTheyMatchTheFilter()
    {
        var list = new PagedTestList(Pages(Page(10, 2)));
        await list.ReloadAsync(new TestFilter(Type: TestType.Custom));

        Assert.False(list.Prepend(new SpeedtestDto { Id = 11, Type = TestType.Auto }));
        Assert.True(list.Prepend(new SpeedtestDto { Id = 12, Type = TestType.Custom }));
        Assert.False(list.Prepend(new SpeedtestDto { Id = 12, Type = TestType.Custom }));

        Assert.Equal([12, 10, 9], list.Items.Select(test => test.Id));
    }

    [Fact]
    public async Task AFailedFirstPage_IsReportedAsALoadFailure()
    {
        var list = new PagedTestList((_, _, _) => throw new SqliteException("database is locked", 5));

        await list.ReloadAsync(TestFilter.None);

        Assert.True(list.LoadFailed);
        Assert.False(list.LoadMoreFailed);
        Assert.False(list.Loading);
    }

    [Fact]
    public async Task AFailedLaterPage_KeepsWhatWasLoaded()
    {
        var pages = 0;
        var list = new PagedTestList((_, _, _) => ++pages == 1
            ? Task.FromResult(Page(100, PagedTestList.PageSize))
            : throw new InvalidOperationException("The database is gone"));
        await list.ReloadAsync(TestFilter.None);

        await list.LoadMoreAsync();

        Assert.True(list.LoadMoreFailed);
        Assert.False(list.LoadFailed);
        Assert.Equal(PagedTestList.PageSize, list.Items.Count);
        Assert.True(list.HasMore);
    }

    [Fact]
    public async Task ADeletedTest_IsRemoved()
    {
        var list = new PagedTestList(Pages(Page(3, 3)));
        await list.ReloadAsync(TestFilter.None);

        list.Remove(2);

        Assert.Equal([3, 1], list.Items.Select(test => test.Id));
    }

    private Func<TestFilter, int?, int, Task<IReadOnlyList<SpeedtestDto>>> Pages(params IReadOnlyList<SpeedtestDto>[] pages) =>
        (filter, afterId, limit) =>
        {
            _requests.Add((filter, afterId, limit));
            return Task.FromResult(_requests.Count <= pages.Length ? pages[_requests.Count - 1] : []);
        };

    private static IReadOnlyList<SpeedtestDto> Page(int newestId, int count) =>
        Enumerable.Range(0, count).Select(offset => new SpeedtestDto { Id = newestId - offset }).ToList();
}
