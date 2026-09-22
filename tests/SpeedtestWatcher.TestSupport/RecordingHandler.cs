using System.Net;

namespace SpeedtestWatcher.TestSupport;

internal sealed class RecordingHandler : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    public HttpStatusCode ResponseStatus { get; set; } = HttpStatusCode.OK;

    public string ResponseBody { get; set; } = "";

    public Exception? Failure { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
        var contentHeaders = request.Content?.Headers.AsEnumerable() ?? [];
        var headers = request.Headers.AsEnumerable()
            .Concat(contentHeaders)
            .Select(header => $"{header.Key}: {string.Join(", ", header.Value)}")
            .Order(StringComparer.Ordinal)
            .ToList();

        Requests.Add(new RecordedRequest(request.Method.Method, request.RequestUri!.ToString(), headers, body));
        if (Failure != null) throw Failure;

        return new HttpResponseMessage(ResponseStatus) { Content = new StringContent(ResponseBody) };
    }
}
