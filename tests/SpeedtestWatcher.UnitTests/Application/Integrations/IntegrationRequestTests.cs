using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Providers;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.TestSupport;
using static SpeedtestWatcher.TestSupport.Approvals;

namespace SpeedtestWatcher.UnitTests.Application.Integrations;

public partial class IntegrationRequestTests
{
    private static readonly JsonSerializerOptions _indentedJson = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private const string AllVariables = "%ping%|%jitter%|%download%|%upload%|%status%|%healthy%|%server%|%threshold_ping%|%threshold_download%|%threshold_upload%|%missed%|%packet_loss%|%bufferbloat%|%data_used%|%error%";

    private static readonly DateTime _tested = new(2026, 9, 16, 8, 5, 0);

    private static readonly Speedtest _healthy = new()
    {
        Id = 41, ServerId = 12345, ServerName = "Acme Fibre", ServerHost = "speed.acme.example",
        Ping = 12, Jitter = 0.4, Download = 941.25, Upload = 110.5, Status = TestStatus.Completed, Healthy = true,
        PacketLoss = 0, DownloadBytes = 903347628, UploadBytes = 88429797, PublicIp = "203.0.113.9",
        Bufferbloat = 18.5, LatencyIdle = 13.2, LatencyLoaded = 31.7, LatencyLoadedTail = 64.2,
        ThresholdPing = 25, ThresholdDownload = 900, ThresholdUpload = 100, Type = TestType.Auto, ResultId = "r-41", Time = 14, Created = _tested
    };

    private static readonly Speedtest _unhealthy = new()
    {
        Id = 42, ServerId = 12345, ServerName = "Acme Fibre", ServerHost = "speed.acme.example",
        Ping = 31, Jitter = 2.75, Download = 612.5, Upload = 98.125, Status = TestStatus.Completed, Healthy = false,
        ThresholdPing = 25, ThresholdDownload = 900, ThresholdUpload = 100, Type = TestType.Custom, ResultId = "r-42", Time = 15, Created = _tested.AddHours(1)
    };

    private static readonly Speedtest _failed = new()
    {
        Id = 44, ServerId = 12345, ServerName = "Acme Fibre", ServerHost = "speed.acme.example",
        Ping = -1, Download = -1, Upload = -1, Status = TestStatus.Failed, Type = TestType.Auto,
        Error = "Network unreachable", Created = _tested.AddHours(3)
    };

    private static readonly Speedtest _skipped = new()
    {
        Id = 43, Ping = -1, Download = -1, Upload = -1, Status = TestStatus.Skipped, Type = TestType.Auto,
        Error = "Public IP 203.0.113.9 is on the skip list", Created = _tested.AddHours(2)
    };

    private static readonly Recommendation _recommendation = new() { Id = 1, Ping = 10, Download = 950.5, Upload = 115.25 };

    private static readonly (string Scenario, string Name, string Settings)[] _scenarios =
    [
        ("discord", "discord", """{"url":"https://localhost/discord.com/api/webhooks/1/x","display_name":"Watcher"}"""),
        ("discord with every template variable", "discord", $$"""{"url":"https://localhost/discord.com/api/webhooks/1/x","finished_message":"{{AllVariables}}","error_message":"{{AllVariables}}","unhealthy_message":"{{AllVariables}}","healthy_again_message":"{{AllVariables}}","skipped_message":"{{AllVariables}}"}"""),
        ("discord without a url", "discord", """{"display_name":"Watcher"}"""),
        ("discord saved from the settings form", "discord", """{"url":"https://localhost/discord.com/api/webhooks/1/x","display_name":"","send_finished":true,"finished_message":"","send_failed":true,"error_message":"","send_unhealthy":true,"unhealthy_message":"","send_healthy_again":true,"healthy_again_message":"","send_skipped":true,"skipped_message":""}"""),
        ("ntfy saved from the settings form", "ntfy", """{"url":"https://localhost/ntfy","topic":"alerts","token":"","title":"","tags":"","priority":"","error_priority":"","send_finished":true,"finished_message":"","send_failed":true,"error_message":"","send_unhealthy":true,"unhealthy_message":"","send_healthy_again":true,"healthy_again_message":"","send_skipped":true,"skipped_message":""}"""),
        ("telegram", "telegram", """{"token":"1:abc","chat_id":"42"}"""),
        ("gotify", "gotify", """{"url":"https://localhost/gotify/","key":"AAAAAAAAAAAAAAA","priority":"4"}"""),
        ("gotify with every message turned off", "gotify", """{"url":"https://localhost/gotify","key":"AAAAAAAAAAAAAAA","send_finished":false,"send_failed":false,"send_unhealthy":false,"send_healthy_again":false,"send_skipped":false,"send_connection_lost":false,"send_connection_restored":false}"""),
        ("ntfy", "ntfy", """{"url":"https://localhost/ntfy","topic":"alerts","token":"tk_1","title":"Speedtest","tags":"warning","priority":"2","error_priority":"4"}"""),
        ("pushover", "pushover", """{"token":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","user_key":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"}"""),
        ("apprise with urls", "apprise", """{"url":"https://localhost/apprise/","urls":"json://localhost/hook, mailto://me@example.com","title":"Speedtest"}"""),
        ("apprise with a config key and tags", "apprise", """{"url":"https://localhost/apprise","key":"home","tags":"admin, devops"}"""),
        ("apprise with urls and a config key", "apprise", """{"url":"https://localhost/apprise","urls":"json://localhost/hook","key":"home"}"""),
        ("apprise saved from the settings form", "apprise", """{"url":"https://localhost/apprise","urls":"","key":"home","tags":"","title":"","send_finished":true,"finished_message":"","send_failed":true,"error_message":"","send_unhealthy":true,"unhealthy_message":"","send_healthy_again":true,"healthy_again_message":"","send_skipped":false,"skipped_message":""}"""),
        ("webhook", "webhook", """{"url":"https://localhost/hook","send_started":true,"send_alive":true,"send_recommendations":true,"send_config_updates":true}"""),
        ("healthChecks", "healthChecks", """{"url":"https://localhost/hc/uuid/"}"""),
        ("influxdb", "influxdb", """{"url":"https://localhost/influx/","org":"home","bucket":"speed","token":"influx-token","host":"watcher"}"""),
        ("influxdb with tags and names that need escaping", "influxdb", """{"url":"https://localhost/influx","org":"home","bucket":"speed","token":"influx-token","measurement":"speed tests","host":"living room","tags":"env=prod, site=home office, broken, host=ignored, =nothing"}"""),
        ("unknown type", "carrierPigeon", """{"url":"https://localhost/coop"}""")
    ];

    [Fact]
    public async Task EveryIntegrationSendsTheApprovedRequests()
    {
        var transcript = new StringBuilder();
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            foreach (var (scenario, name, settings) in _scenarios)
            {
                foreach (var (eventName, publish) in Events())
                {
                    var handler = new RecordingHandler();
                    var repository = new InMemoryIntegrations([new IntegrationData { Id = "abc123", Name = name, Data = settings }]);
                    var dispatcher = TestIntegrations.Dispatcher(repository, handler);

                    await publish(dispatcher, TestContext.Current.CancellationToken);

                    Record(transcript, $"{scenario} · {eventName}", $"activity: {Activity(repository.ActivityErrors)}", handler);
                }

                var testHandler = new RecordingHandler { ResponseBody = """{"buckets":[{"name":"speed"}]}""" };
                var testRepository = new InMemoryIntegrations([]);
                var result = await TestIntegrations.Dispatcher(testRepository, testHandler)
                    .TestAsync(name, "abc123", settings, _healthy, TestContext.Current.CancellationToken);

                Record(transcript, $"{scenario} · send test", $"result: {TestResult(result)}, activity: {Activity(testRepository.ActivityErrors)}", testHandler);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        AssertMatchesApproved("IntegrationRequests.approved.txt", transcript.ToString());
    }

    [Fact]
    public void TheSettingsFormsForEveryIntegrationTypeStayTheSame()
    {
        var dispatcher = TestIntegrations.Dispatcher(new InMemoryIntegrations([]), new RecordingHandler());

        var schemas = JsonSerializer.Serialize(dispatcher.Schemas, _indentedJson);

        AssertMatchesApproved("IntegrationSchemas.approved.json", schemas + "\n");
    }

    private static IEnumerable<(string Name, Func<IntegrationDispatcher, CancellationToken, Task> Publish)> Events() =>
    [
        ("test started", (dispatcher, ct) => dispatcher.PublishAsync(new TestStarted(SpeedtestProvider.Ookla, TestType.Auto), ct)),
        ("test finished", (dispatcher, ct) => dispatcher.PublishAsync(new TestFinished(_healthy), ct)),
        ("test missed targets", (dispatcher, ct) => dispatcher.PublishAsync(new TestUnhealthy(_unhealthy), ct)),
        ("test met targets again", (dispatcher, ct) => dispatcher.PublishAsync(new TestHealthyAgain(_healthy), ct)),
        ("test failed", (dispatcher, ct) => dispatcher.PublishAsync(new TestFailed(_failed), ct)),
        ("test skipped", (dispatcher, ct) => dispatcher.PublishAsync(new TestSkipped(_skipped), ct)),
        ("connection lost", (dispatcher, ct) => dispatcher.PublishAsync(new ConnectionLost(_tested.AddHours(4)), ct)),
        ("connection restored", (dispatcher, ct) => dispatcher.PublishAsync(new ConnectionRestored(_tested.AddHours(4).AddMinutes(7), TimeSpan.FromMinutes(7)), ct)),
        ("recommendations updated", (dispatcher, ct) => dispatcher.PublishAsync(new RecommendationsUpdated(_recommendation), ct)),
        ("config updated", (dispatcher, ct) => dispatcher.PublishAsync(new ConfigUpdated("cron", "0 * * * *"), ct)),
        ("heartbeat", (dispatcher, ct) => dispatcher.PublishAsync(new Heartbeat(), ct))
    ];

    private static void Record(StringBuilder transcript, string heading, string outcome, RecordingHandler handler)
    {
        transcript.Append("## ").AppendLine(heading);
        transcript.AppendLine(outcome);
        foreach (var request in handler.Requests)
        {
            transcript.Append(request.Method).Append(' ').AppendLine(request.Uri);
            foreach (var header in request.Headers) transcript.AppendLine(header);
            transcript.AppendLine(MaskSendTime(request.Body));
        }
        transcript.AppendLine();
    }

    private static string TestResult(IntegrationResult result) => result.Outcome switch
    {
        IntegrationOutcome.Failed => $"failed ({result.Error})",
        var outcome => outcome.ToString().ToLowerInvariant()
    };

    private static string Activity(IReadOnlyList<bool> errors) => errors switch
    {
        [] => "unchanged",
        [false] => "sent",
        [true] => "failed",
        _ => string.Join(",", errors.Select(error => error ? "failed" : "sent"))
    };

    private static string MaskSendTime(string body)
    {
        var withoutIsoTime = SendTimeIso().Replace(body, "<send time>");
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return UnixSeconds().Replace(withoutIsoTime, match =>
            Math.Abs(long.Parse(match.Value, CultureInfo.InvariantCulture) - now) < 3600 ? "<send time>" : match.Value);
    }

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+Z")]
    private static partial Regex SendTimeIso();

    [GeneratedRegex(@"\b\d{10}\b")]
    private static partial Regex UnixSeconds();
}
