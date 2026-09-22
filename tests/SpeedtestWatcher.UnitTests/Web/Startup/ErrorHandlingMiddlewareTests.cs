using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.TestSupport;
using SpeedtestWatcher.Web.Startup;

namespace SpeedtestWatcher.UnitTests.Web.Startup;

public sealed class ErrorHandlingMiddlewareTests
{
    private const string InternalDetail = "SQLite Error 14: unable to open database file /app/data/storage.db";

    private readonly RecordingLogger<ErrorHandlingMiddleware> _logger = new();

    [Fact]
    public async Task AnUnexpectedApiError_AnswersWithAGenericMessage_AndLogsTheDetails()
    {
        var context = Request("/api/storage");
        var middleware = new ErrorHandlingMiddleware(_ => throw new InvalidOperationException(InternalDetail), _logger);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(ErrorHandlingMiddleware.UnexpectedErrorMessage, body.RootElement.GetProperty("message").GetString());
        Assert.Equal(InternalDetail, Assert.Single(_logger.Entries, entry => entry.Level == LogLevel.Error).Exception?.Message);
    }

    [Fact]
    public async Task AnUnexpectedPageError_IsLeftToTheErrorPage()
    {
        var middleware = new ErrorHandlingMiddleware(_ => throw new InvalidOperationException(InternalDetail), _logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(Request("/history")));
    }

    private static DefaultHttpContext Request(string path) => new()
    {
        Request = { Path = path },
        Response = { Body = new MemoryStream() }
    };
}
