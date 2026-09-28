namespace SpeedtestWatcher.Application.Configuration;

public sealed record PreTestCheckSettings(bool InternetCheckEnabled, string InternetCheckUrl, IReadOnlyList<string> SkipIps);
