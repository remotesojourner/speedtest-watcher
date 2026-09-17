using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpeedtestWatcher.Core.Integrations;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Integrations;

namespace SpeedtestWatcher.Tests;

internal static class TestIntegrations
{
    public static IntegrationDispatcher Dispatcher(IIntegrationRepository repository, HttpMessageHandler handler, ILogger<IntegrationDispatcher>? logger = null) =>
        new(repository, All(handler), new HeartbeatSchedule(), logger ?? NullLogger<IntegrationDispatcher>.Instance);

    public static IReadOnlyList<IIntegration> All(HttpMessageHandler handler) =>
        new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(handler))
            .AddIntegrations()
            .BuildServiceProvider()
            .GetServices<IIntegration>()
            .ToList();
}

internal sealed record RecordedRequest(string Method, string Uri, IReadOnlyList<string> Headers, string Body);

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

internal sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}

internal sealed class InMemoryIntegrations(List<IntegrationData> items) : IIntegrationRepository
{
    public List<bool> ActivityErrors { get; } = [];

    public Task<List<IntegrationData>> ListAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(items);

    public Task UpdateActivityAsync(string id, bool error, CancellationToken cancellationToken = default)
    {
        ActivityErrors.Add(error);
        return Task.CompletedTask;
    }

    public Task<List<IntegrationData>> GetByNameAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IntegrationData?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<string> CreateAsync(string name, string displayName, string dataJson, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> PatchAsync(string id, string? displayName, string dataJson, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UpsertAsync(IntegrationData integration, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task ClearAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
