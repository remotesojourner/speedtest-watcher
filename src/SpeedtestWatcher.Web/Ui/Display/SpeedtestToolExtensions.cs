using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Providers;

namespace SpeedtestWatcher.Web.Ui.Display;

public static class SpeedtestToolExtensions
{
    public static string ImagePath(this ISpeedtestTool tool) => $"img/{tool.Provider.ToName()}.webp";

    public static bool SupportsServerChoice(this ISpeedtestTool tool) => tool.Servers != null;
}
