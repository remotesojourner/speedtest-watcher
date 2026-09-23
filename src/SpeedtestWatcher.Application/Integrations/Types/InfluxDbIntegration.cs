using System.Globalization;
using System.Text;
using System.Text.Json;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations.Types;

internal sealed class InfluxDbIntegration : HttpIntegration
{
    private const string HostTag = "host";
    private const string MissingDestination = "The URL, organization or bucket is missing";

    public InfluxDbIntegration(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "influxdb";

    public override IntegrationTypeSchemaDto Schema { get; } = new()
    {
        Name = "influxdb",
        Title = "InfluxDB v2",
        Description = "Export metrics to InfluxDB v2 via Line Protocol",
        Fields =
        [
            new() { Name = "url", Type = "text", Required = true, Regex = @"^https?://.+", Placeholder = "http://localhost:8086" },
            new() { Name = "org", Type = "text", Required = true, Placeholder = "organization" },
            new() { Name = "bucket", Type = "text", Required = true, Placeholder = "bucket" },
            new() { Name = "token", Type = "text", Required = true, Placeholder = "api-token" },
            new() { Name = "measurement", Type = "text", Required = false, Default = "speedtests" },
            new() { Name = "host", Type = "text", Required = false },
            new() { Name = "tags", Type = "text", Required = false, Placeholder = "env=prod,server=node1" }
        ]
    };

    public override Task<IntegrationResult> HandleAsync(IntegrationEvent integrationEvent, IntegrationContext context, CancellationToken cancellationToken)
    {
        if (integrationEvent is not TestFinished { Result: var test }) return Task.FromResult(IntegrationResult.NotApplicable);

        if (Destination.From(context.Settings) is not { } destination) return Task.FromResult(IntegrationResult.Failed(MissingDestination));

        var request = TextPost($"{destination.Url}/api/v2/write?{destination.OrgQuery}&bucket={Uri.EscapeDataString(destination.Bucket)}&precision=s", Line(test, context.Settings));
        return SendAsync(Authorized(request, context.Settings), cancellationToken);
    }

    public override Task<IntegrationResult> SendTestAsync(IntegrationContext context, Speedtest sample, CancellationToken cancellationToken)
    {
        if (Destination.From(context.Settings) is not { } destination) return Task.FromResult(IntegrationResult.Failed(MissingDestination));

        var request = new HttpRequestMessage(HttpMethod.Get, $"{destination.Url}/api/v2/buckets?{destination.OrgQuery}&name={Uri.EscapeDataString(destination.Bucket)}");
        return SendAsync(Authorized(request, context.Settings), cancellationToken, (status, reply) =>
            IsSuccess(status) && !BucketExists(reply, destination.Bucket)
                ? IntegrationResult.Failed($"InfluxDB has no bucket named {destination.Bucket} in {destination.Org}")
                : null);
    }

    private static HttpRequestMessage Authorized(HttpRequestMessage request, IntegrationSettings settings)
    {
        request.Headers.Add("Authorization", $"Token {settings.GetString("token")}");
        return request;
    }

    private static bool BucketExists(string reply, string bucket)
    {
        try
        {
            using var document = JsonDocument.Parse(reply);
            return document.RootElement.TryGetProperty("buckets", out var buckets)
                   && buckets.ValueKind == JsonValueKind.Array
                   && buckets.EnumerateArray().Any(found => found.TryGetProperty("name", out var name) && name.GetString() == bucket);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record Destination(string Url, string Org, string Bucket)
    {
        public string OrgQuery => $"org={Uri.EscapeDataString(Org)}";

        public static Destination? From(IntegrationSettings settings)
        {
            var destination = new Destination(settings.GetString("url").TrimEnd('/'), settings.GetString("org"), settings.GetString("bucket"));
            return string.IsNullOrEmpty(destination.Url) || string.IsNullOrEmpty(destination.Org) || string.IsNullOrEmpty(destination.Bucket)
                ? null
                : destination;
        }
    }

    private static string Line(Speedtest test, IntegrationSettings settings)
    {
        var line = new StringBuilder(EscapeMeasurement(settings.GetString("measurement", "speedtests")));
        foreach (var (key, value) in Tags(settings))
        {
            line.Append(',').Append(EscapeTagPart(key)).Append('=').Append(EscapeTagPart(value));
        }

        var testedAt = new DateTimeOffset(test.Created).ToUnixTimeSeconds();
        line.Append(CultureInfo.InvariantCulture,
            $" download={test.Download:F2},upload={test.Upload:F2},ping={(double)test.Ping:F0},jitter={(test.Jitter ?? 0):F2}");

        if (test.PacketLoss is { } packetLoss) line.Append(CultureInfo.InvariantCulture, $",packet_loss={packetLoss:F2}");
        if (test.Bufferbloat is { } bufferbloat) line.Append(CultureInfo.InvariantCulture, $",bufferbloat={bufferbloat:F2}");
        if (test.DownloadBytes + test.UploadBytes is { } bytes) line.Append(CultureInfo.InvariantCulture, $",bytes={bytes}i");

        return line.Append(CultureInfo.InvariantCulture, $" {testedAt}").ToString();
    }

    private static IEnumerable<(string Key, string Value)> Tags(IntegrationSettings settings)
    {
        yield return (HostTag, settings.GetString(HostTag, Environment.MachineName));

        foreach (var pair in settings.GetString("tags").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0 || separator == pair.Length - 1) continue;

            var key = pair[..separator].Trim();
            var value = pair[(separator + 1)..].Trim();
            if (key.Length == 0 || value.Length == 0 || key.Equals(HostTag, StringComparison.OrdinalIgnoreCase)) continue;

            yield return (key, value);
        }
    }

    private static string EscapeMeasurement(string measurement) =>
        measurement.Replace(",", "\\,").Replace(" ", "\\ ");

    private static string EscapeTagPart(string part) =>
        part.Replace(",", "\\,").Replace("=", "\\=").Replace(" ", "\\ ");
}
