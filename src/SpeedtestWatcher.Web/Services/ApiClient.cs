using System.Net;
using System.Text;
using System.Text.Json;

namespace SpeedtestWatcher.Web.Services;

public record ApiResult(HttpStatusCode? Status, string? ServerMessage)
{
    public static readonly ApiResult Ok = new(HttpStatusCode.OK, null);

    public bool Succeeded => Status is { } status && (int)status is >= 200 and < 300;

    public string MessageOr(string fallback) =>
        Status is { } status && (int)status is >= 400 and < 500 && !string.IsNullOrWhiteSpace(ServerMessage)
            ? ServerMessage
            : fallback;
}

public sealed record ApiResult<T>(T? Value, HttpStatusCode? Status, string? ServerMessage) : ApiResult(Status, ServerMessage);

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient http, ILogger<ApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Uri BaseAddress => _http.BaseAddress!;

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync(HttpMethod.Get, url, null, ReadJsonAsync<T>);

    public Task<ApiResult<byte[]>> GetBytesAsync(string url) => SendAsync(HttpMethod.Get, url, null, ReadBytesAsync);

    public Task<ApiResult<byte[]>> PostForBytesAsync(string url, object body) => SendAsync(HttpMethod.Post, url, JsonBody(body), ReadBytesAsync);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) => SendAsync(HttpMethod.Post, url, JsonBody(body), ReadJsonAsync<T>);

    public Task<ApiResult> PostAsync(string url, object? body = null) => SendWithoutValueAsync(HttpMethod.Post, url, JsonBody(body));

    public Task<ApiResult<T>> PutAsync<T>(string url, object body) => SendAsync(HttpMethod.Put, url, JsonBody(body), ReadJsonAsync<T>);

    public Task<ApiResult> PutJsonAsync(string url, string json) =>
        SendWithoutValueAsync(HttpMethod.Put, url, new StringContent(json, Encoding.UTF8, "application/json"));

    public Task<ApiResult<T>> PutJsonAsync<T>(string url, string json) =>
        SendAsync(HttpMethod.Put, url, new StringContent(json, Encoding.UTF8, "application/json"), ReadJsonAsync<T>);

    public Task<ApiResult> PatchAsync(string url, object body) => SendWithoutValueAsync(HttpMethod.Patch, url, JsonBody(body));

    public Task<ApiResult> DeleteAsync(string url) => SendWithoutValueAsync(HttpMethod.Delete, url, null);

    private async Task<ApiResult> SendWithoutValueAsync(HttpMethod method, string url, HttpContent? content) =>
        await SendAsync(method, url, content, _ => Task.FromResult(true));

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content, Func<HttpContent, Task<T?>> readValue)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            request.Content = content;
            using var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return new ApiResult<T>(await readValue(response.Content), response.StatusCode, null);
            }

            var message = await ReadServerMessageAsync(response.Content);
            var level = (int)response.StatusCode >= 500 ? LogLevel.Warning : LogLevel.Debug;
            _logger.Log(level, "{Method} {Url} answered {Status}: {Message}", method, url, (int)response.StatusCode, message);
            return new ApiResult<T>(default, response.StatusCode, message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
        {
            _logger.LogWarning(ex, "{Method} {Url} failed", method, url);
            return new ApiResult<T>(default, null, null);
        }
    }

    private static HttpContent? JsonBody(object? body) => body == null ? null : JsonContent.Create(body, body.GetType());

    private static Task<T?> ReadJsonAsync<T>(HttpContent content) => content.ReadFromJsonAsync<T>();

    private static async Task<byte[]?> ReadBytesAsync(HttpContent content) => await content.ReadAsByteArrayAsync();

    private static async Task<string?> ReadServerMessageAsync(HttpContent content)
    {
        var body = await content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.ValueKind == JsonValueKind.Object
                   && document.RootElement.TryGetProperty("message", out var message)
                   && message.ValueKind == JsonValueKind.String
                ? message.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
