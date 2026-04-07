using ErrorOr;

namespace Shoplists.Api.Extensions;

internal static class ErrorOrExtensions
{
    internal static async Task<IResult> ToHttpResult<T>(
        this ValueTask<ErrorOr<T>> resultTask,
        Func<T, IResult>? onSuccess = null
    )
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.ToHttpResult(onSuccess);
    }

    private static IResult ToHttpResult<T>(
        this ErrorOr<T> result,
        Func<T, IResult>? onSuccess = null
    )
    {
        if (result.IsError)
        {
            return ToErrorResult(result.Errors);
        }

        var successMapper = onSuccess ?? TypedResults.Ok;
        return successMapper(result.Value);
    }

    private static IResult ToErrorResult(List<Error> errors)
    {
        if (errors.TrueForAll(e => e.Type == ErrorType.Validation))
        {
            var validationErrors = errors
                .GroupBy(e => e.Code, StringComparer.Ordinal)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray(),
                    StringComparer.Ordinal
                );

            return TypedResults.ValidationProblem(validationErrors);
        }

        var firstError = errors[0];
        var statusCode = firstError.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status403Forbidden,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };

        return TypedResults.Problem(
            statusCode: statusCode,
            title: firstError.Code,
            detail: firstError.Description
        );
    }
}
