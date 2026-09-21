namespace SpeedtestWatcher.Core.Settings;

public sealed record PreTestCheckSettings(bool InternetCheckEnabled, string InternetCheckUrl, IReadOnlyList<string> SkipIps);
