using SpeedtestWatcher.Web.Api.Contracts;

namespace SpeedtestWatcher.Web.Startup;

public partial class ErrorHandlingMiddleware
{
    public const string UnexpectedErrorMessage = "Something went wrong on the server. The details are in the Speedtest Watcher log.";

    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            LogUnhandled(ex);

            if (!context.Response.HasStarted && context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new ErrorResponse { Message = UnexpectedErrorMessage });
            }
            else
            {
                throw;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception occurred")]
    private partial void LogUnhandled(Exception exception);
}
