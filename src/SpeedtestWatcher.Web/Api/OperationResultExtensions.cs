using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application;

namespace SpeedtestWatcher.Web.Api;

public static class OperationResultExtensions
{
    public static IActionResult ToActionResult(this OperationResult result, string successMessage) =>
        result.Succeeded ? new OkObjectResult(new { message = successMessage }) : Failure(result.Outcome, result.Message);

    public static IActionResult ToActionResult<T>(this OperationResult<T> result) =>
        result.Succeeded ? new OkObjectResult(result.Value) : Failure(result.Outcome, result.Message);

    public static IActionResult ToActionResult<T>(this OperationResult<T> result, Func<T, IActionResult> success) =>
        result.Succeeded ? success(result.Value!) : Failure(result.Outcome, result.Message);

    private static ObjectResult Failure(OperationOutcome outcome, string? message) => new(new { message })
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
