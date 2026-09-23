using SpeedtestWatcher.Application.Resources;
using System.Net;
using System.Text;
using System.Text.Json;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Integrations;

internal abstract class HttpIntegration : IIntegration
{
    private const int ReplyExcerptLength = 200;
    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(15);

    private readonly IHttpClientFactory _httpClientFactory;

    protected HttpIntegration(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public abstract string Name { get; }

    public abstract IntegrationTypeSchemaDto Schema { get; }

    public abstract Task<IntegrationResult> HandleAsync(IntegrationEvent integrationEvent, IntegrationContext context, CancellationToken cancellationToken);

    public abstract Task<IntegrationResult> SendTestAsync(IntegrationContext context, Speedtest sample, CancellationToken cancellationToken);

    protected async Task<IntegrationResult> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Func<HttpStatusCode, string, IntegrationResult?>? judgeReply = null)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = _requestTimeout;
        try
        {
            using (request)
            using (var response = await client.SendAsync(request, cancellationToken))
            {
                var reply = await response.Content.ReadAsStringAsync(cancellationToken);
                if (judgeReply?.Invoke(response.StatusCode, reply) is { } judged) return judged;

                return response.IsSuccessStatusCode ? IntegrationResult.Sent : IntegrationResult.Failed(Answered(response.StatusCode, reply));
            }
        }
        catch (HttpRequestException ex)
        {
            return IntegrationResult.Failed(ApplicationStrings.Format(ApplicationStrings.IntegrationUnreachable, Schema.Title, ex.Message));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return IntegrationResult.Failed(ApplicationStrings.Format(ApplicationStrings.IntegrationTimeout, Schema.Title, _requestTimeout.TotalSeconds));
        }
    }

    protected static HttpRequestMessage JsonPost(string url, object? payload) => new(HttpMethod.Post, url)
    {
        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
    };

    protected static HttpRequestMessage TextPost(string url, string text) => new(HttpMethod.Post, url)
    {
        Content = new StringContent(text, Encoding.UTF8, "text/plain")
    };

    protected static bool IsSuccess(HttpStatusCode status) => (int)status is >= 200 and < 300;

    protected string Answered(HttpStatusCode status, string reply)
    {
        var excerpt = Excerpt(reply);
        return excerpt.Length == 0
            ? ApplicationStrings.Format(ApplicationStrings.IntegrationAnswered, Schema.Title, (int)status)
            : ApplicationStrings.Format(ApplicationStrings.IntegrationAnsweredWithReply, Schema.Title, (int)status, excerpt);
    }

    private static string Excerpt(string reply)
    {
        var singleLine = string.Join(' ', reply.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return singleLine.Length <= ReplyExcerptLength ? singleLine : singleLine[..ReplyExcerptLength] + "…";
    }
}
