namespace SpeedtestWatcher.Application.Common;

public sealed class OperationResult<T>
{
    internal OperationResult(OperationOutcome outcome, string? message, T? value)
    {
        Outcome = outcome;
        Message = message;
        Value = value;
    }

    public OperationOutcome Outcome { get; }

    public string? Message { get; }

    public T? Value { get; }

    public bool Succeeded => Outcome == OperationOutcome.Ok;

    public static implicit operator OperationResult<T>(OperationResult result) => new(result.Outcome, result.Message, default);
}
