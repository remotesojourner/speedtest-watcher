using System.Net.NetworkInformation;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.Services;

internal sealed class NetworkTrafficService : INetworkTrafficService
{
    public TrafficCounters Read()
    {
        long received = 0;
        long sent = 0;

        foreach (var card in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (card.OperationalStatus != OperationalStatus.Up || card.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            try
            {
                var statistics = card.GetIPStatistics();
                received += statistics.BytesReceived;
                sent += statistics.BytesSent;
            }
            catch (NetworkInformationException)
            {
                continue;
            }
        }

        return new TrafficCounters(received, sent);
    }
}
