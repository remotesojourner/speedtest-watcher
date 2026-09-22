using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Web.Api.Contracts;

namespace SpeedtestWatcher.Web.Api;

public static class OperationResultExtensions
{
    public const string UnexplainedFailure = "The request couldn't be completed";

    public static ActionResult<MessageResponse> ToActionResult(this OperationResult result, string successMessage) =>
        result.Succeeded ? new OkObjectResult(new MessageResponse { Message = successMessage }) : Failure(result.Outcome, result.Message);

    public static ActionResult<TResponse> ToActionResult<T, TResponse>(this OperationResult<T> result, Func<T, TResponse> toResponse) =>
        result.Succeeded ? new OkObjectResult(toResponse(result.Value!)) : Failure(result.Outcome, result.Message);

    public static IActionResult ToFileResult(this OperationResult<ExportFile> result) =>
        result.Succeeded ? ToFileResult(result.Value!) : Failure(result.Outcome, result.Message);

    public static FileContentResult ToFileResult(this ExportFile file) => new(file.Content, file.ContentType) { FileDownloadName = file.FileName };

    private static ObjectResult Failure(OperationOutcome outcome, string? message) => new(new ErrorResponse { Message = message ?? UnexplainedFailure })
    {
        StatusCode = outcome switch
        {
            OperationOutcome.Invalid => StatusCodes.Status400BadRequest,
            OperationOutcome.NotFound => StatusCodes.Status404NotFound,
            OperationOutcome.Conflict => StatusCodes.Status409Conflict,
            OperationOutcome.Denied => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        }
    };
}
