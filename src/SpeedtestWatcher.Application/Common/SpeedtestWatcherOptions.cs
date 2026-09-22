namespace SpeedtestWatcher.Application.Common;

public sealed class SpeedtestWatcherOptions
{
    public int Port { get; set; } = 2003;

    public string DataDirectory { get; set; } = "data";

    public string BinDirectory { get; set; } = "bin";

    public bool DisableAuth { get; set; }

    public bool RunTestOnStartup { get; set; }

    public string DatabasePath => Path.Combine(DataDirectory, "storage.db");

    public string ServersDirectory => Path.Combine(DataDirectory, "servers");

    public string KeysDirectory => Path.Combine(DataDirectory, "keys");
}
