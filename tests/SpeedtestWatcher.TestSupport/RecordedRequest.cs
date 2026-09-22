namespace SpeedtestWatcher.TestSupport;

internal sealed record RecordedRequest(string Method, string Uri, IReadOnlyList<string> Headers, string Body);
