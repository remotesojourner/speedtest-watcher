using System.Net.NetworkInformation;

namespace SpeedtestWatcher.Application.Monitoring;

public readonly record struct TrafficCounters(long Received, long Sent)
{
    public static TrafficCounters None { get; }

    public TrafficCounters Since(TrafficCounters earlier) => new(Received - earlier.Received, Sent - earlier.Sent);
}

public interface INetworkTraffic
{
    TrafficCounters Read();
}

internal sealed class NetworkInterfaceTraffic : INetworkTraffic
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
