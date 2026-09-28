using SpeedtestWatcher.Application.Extensions;
using SpeedtestWatcher.Application.Services.Providers.Interfaces;

namespace SpeedtestWatcher.Web.Extensions;

public static class SpeedtestProviderServiceExtensions
{
    public static string ImagePath(this ISpeedtestProviderService providerService) => $"img/{providerService.Provider.ToName()}.webp";

    public static bool SupportsServerChoice(this ISpeedtestProviderService providerService) => providerService.Servers != null;
}
