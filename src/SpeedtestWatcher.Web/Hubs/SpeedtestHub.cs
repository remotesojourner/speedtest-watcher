using Microsoft.AspNetCore.SignalR;

namespace SpeedtestWatcher.Web.Hubs;

public class SpeedtestHub : Hub
{
    public const string HubUrl = "/speedtestHub";
}
