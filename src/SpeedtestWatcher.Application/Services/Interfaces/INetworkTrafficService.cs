using SpeedtestWatcher.Application.Models;

namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface INetworkTrafficService
{
    TrafficCounters Read();
}
