using Microsoft.AspNetCore.Mvc.ModelBinding;
using SpeedtestWatcher.Web.Api.Contracts;

namespace SpeedtestWatcher.Web.Api;

public static class InvalidRequest
{
    public const string Summary = "The request isn't valid.";

    public static ErrorResponse Describe(ModelStateDictionary modelState)
    {
        var problems = modelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .SelectMany(entry => entry.Value!.Errors.Select(error => Problem(entry.Key, error)))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new ErrorResponse { Message = problems.Count == 0 ? Summary : $"{Summary} {string.Join(" ", problems)}" };
    }

    private static string Problem(string field, ModelError error)
    {
        var text = string.IsNullOrWhiteSpace(error.ErrorMessage) ? "A value couldn't be read." : error.ErrorMessage.Trim();
        return string.IsNullOrEmpty(field) || text.Contains(field, StringComparison.OrdinalIgnoreCase) ? text : $"{field}: {text}";
    }
}
