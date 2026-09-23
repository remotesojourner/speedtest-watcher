using SpeedtestWatcher.Application.Resources;

namespace SpeedtestWatcher.Application.Common;

public class OperationResult
{
    public static string DeniedMessage => ApplicationStrings.AuthenticationRequired;

    protected OperationResult(OperationOutcome outcome, string? message)
    {
        Outcome = outcome;
        Message = message;
    }

    public OperationOutcome Outcome { get; }

    public string? Message { get; }

    public bool Succeeded => Outcome == OperationOutcome.Ok;

    public static OperationResult Ok() => new(OperationOutcome.Ok, null);

    public static OperationResult<T> Ok<T>(T value) => new(OperationOutcome.Ok, null, value);

    public static OperationResult Invalid(string message) => new(OperationOutcome.Invalid, message);

    public static OperationResult NotFound(string message) => new(OperationOutcome.NotFound, message);

    public static OperationResult Conflict(string message) => new(OperationOutcome.Conflict, message);

    public static OperationResult Denied() => new(OperationOutcome.Denied, DeniedMessage);
}
