using SpeedtestWatcher.Application.Extensions;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Web.Extensions;

public static class SpeedtestProviderServiceExtensions
{
    public static string ImagePath(this ISpeedtestProviderService tool) => $"img/{tool.Provider.ToName()}.webp";

    public static bool SupportsServerChoice(this ISpeedtestProviderService tool) => tool.Servers != null;
}
