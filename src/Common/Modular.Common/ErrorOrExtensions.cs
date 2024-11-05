using ErrorOr;

namespace Modular.Common;

public static class ErrorOrExtensions
{
    public static IResult ToResult<T>(this ErrorOr<T> response, Func<T, IResult> onSuccess)
    {
        if (!response.IsError)
        {
            return onSuccess(response.Value);
        }

        return response.FirstError.Type switch
        {
            ErrorType.Validation => Results.Problem(statusCode: 400, title: "Bad Request", detail: response.FirstError.Description),
            ErrorType.NotFound => Results.Problem(statusCode: 404, title: "Not Found", detail: response.FirstError.Description),
            ErrorType.Failure => Results.Problem(statusCode: 500, detail: response.FirstError.Description),
            _ => Results.Problem(statusCode: 500, title: "Server Error", detail: "An unexpected error occurred.")
        };

    }
}
