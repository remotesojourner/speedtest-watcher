using SpeedtestWatcher.Core.Helpers;
using SpeedtestWatcher.Core.SpeedTest;

namespace SpeedtestWatcher.Web.Helpers;

public static class SpeedtestToolExtensions
{
    public static string ImagePath(this ISpeedtestTool tool) => $"img/{tool.Provider.ToName()}.webp";

    public static bool SupportsServerChoice(this ISpeedtestTool tool) => tool.Servers != null;
}
