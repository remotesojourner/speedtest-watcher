using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Web.Services;

public sealed record TestFilter(TestStatus? Status = null, TestType? Type = null, bool? Healthy = null)
{
    public static TestFilter None { get; } = new();

    public bool IsActive => Status != null || Type != null || Healthy != null;

    public bool Matches(SpeedtestDto test) =>
        (Status == null || test.Status == Status)
        && (Type == null || test.Type == Type)
        && (Healthy == null || test.Healthy == Healthy);
}
