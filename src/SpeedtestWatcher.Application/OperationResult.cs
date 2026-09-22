namespace SpeedtestWatcher.Application;

public class OperationResult
{
    public const string DeniedMessage = "Authentication required";

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
