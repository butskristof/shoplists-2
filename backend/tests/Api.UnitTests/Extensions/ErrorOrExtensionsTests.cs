using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Shoplists.Api.Extensions;

namespace Shoplists.Api.UnitTests.Extensions;

// CA2012: AsResultTask wraps an already-computed ErrorOr in a synchronously-completed ValueTask
// (via ValueTask.FromResult — never a pooled IValueTaskSource), and each instance is consumed
// exactly once by the ToHttpResult extension under test. The analyzer can't see that
// single-consumption guarantee through the extension call, but it holds, so suppression is safe.
#pragma warning disable CA2012 // Use ValueTasks correctly
public sealed class ErrorOrExtensionsTests
{
    private sealed record TestValue(int Id);

    private static ValueTask<ErrorOr<TestValue>> AsResultTask(ErrorOr<TestValue> result) =>
        ValueTask.FromResult(result);

    [Test]
    public async Task ToHttpResult_Success_DefaultsToOk()
    {
        ErrorOr<TestValue> result = new TestValue(42);

        var httpResult = await AsResultTask(result).ToHttpResult();

        await Assert.That(httpResult).IsTypeOf<Ok<TestValue>>();
        var ok = (Ok<TestValue>)httpResult;
        await Assert.That(ok.StatusCode).IsEqualTo(StatusCodes.Status200OK);
        await Assert.That(ok.Value).IsEqualTo(result.Value);
    }

    [Test]
    public async Task ToHttpResult_Success_WithOnSuccessOverride_UsesIt()
    {
        ErrorOr<TestValue> result = new TestValue(7);

        var httpResult = await AsResultTask(result)
            .ToHttpResult(value => TypedResults.Created($"/test/{value.Id}", value));

        await Assert.That(httpResult).IsTypeOf<Created<TestValue>>();
        var created = (Created<TestValue>)httpResult;
        await Assert.That(created.StatusCode).IsEqualTo(StatusCodes.Status201Created);
        await Assert.That(created.Value).IsEqualTo(result.Value);
    }

    [Test]
    public async Task ToHttpResult_Success_WithAsyncOnSuccess_AwaitsAndUsesIt()
    {
        ErrorOr<TestValue> result = new TestValue(13);

        var httpResult = await AsResultTask(result)
            .ToHttpResult(value => Task.FromResult<IResult>(TypedResults.Ok(value)));

        await Assert.That(httpResult).IsTypeOf<Ok<TestValue>>();
        var ok = (Ok<TestValue>)httpResult;
        await Assert.That(ok.Value).IsEqualTo(result.Value);
    }

    [Test]
    public async Task ToHttpResult_Error_WithAsyncOnSuccess_DoesNotInvokeOnSuccess()
    {
        ErrorOr<TestValue> result = Error.NotFound();
        var onSuccessInvoked = false;

        var httpResult = await AsResultTask(result)
            .ToHttpResult(value =>
            {
                onSuccessInvoked = true;
                return Task.FromResult<IResult>(TypedResults.Ok(value));
            });

        await Assert.That(onSuccessInvoked).IsFalse();
        await Assert.That(httpResult).IsTypeOf<ProblemHttpResult>();
    }

    [Test]
    public async Task ToHttpResult_AllValidationErrors_ReturnsValidationProblemGroupedByCode()
    {
        ErrorOr<TestValue> result = new List<Error>
        {
            Error.Validation("Name", "Name is required"),
            Error.Validation("Name", "Name is too short"),
            Error.Validation("Quantity", "Quantity must be positive"),
        };

        var httpResult = await AsResultTask(result).ToHttpResult();

        await Assert.That(httpResult).IsTypeOf<ValidationProblem>();
        var validationProblem = (ValidationProblem)httpResult;
        await Assert.That(validationProblem.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);

        var errors = validationProblem.ProblemDetails.Errors;
        await Assert.That(errors.Count).IsEqualTo(2);
        await Assert
            .That(errors["Name"])
            .IsEquivalentTo(new[] { "Name is required", "Name is too short" });
        await Assert.That(errors["Quantity"]).IsEquivalentTo(new[] { "Quantity must be positive" });
    }

    public static IEnumerable<(Error Error, int ExpectedStatusCode)> NonValidationErrors()
    {
        yield return (Error.NotFound(), StatusCodes.Status404NotFound);
        yield return (Error.Conflict(), StatusCodes.Status409Conflict);
        yield return (Error.Unauthorized(), StatusCodes.Status403Forbidden);
    }

    [Test]
    [MethodDataSource(nameof(NonValidationErrors))]
    public async Task ToHttpResult_NonValidationError_MapsToExpectedStatusCode(
        Error error,
        int expectedStatusCode
    )
    {
        ErrorOr<TestValue> result = error;

        var httpResult = await AsResultTask(result).ToHttpResult();

        await Assert.That(httpResult).IsTypeOf<ProblemHttpResult>();
        var problem = (ProblemHttpResult)httpResult;
        await Assert.That(problem.StatusCode).IsEqualTo(expectedStatusCode);
        await Assert.That(problem.ProblemDetails.Title).IsEqualTo(error.Code);
        await Assert.That(problem.ProblemDetails.Detail).IsEqualTo(error.Description);
    }

    [Test]
    public async Task ToHttpResult_MixedErrorsLedByValidation_MapsToBadRequestProblem()
    {
        // Not all errors are validation errors, so the all-validation grouping branch is skipped
        // and the first error's type drives the status code via the switch.
        ErrorOr<TestValue> result = new List<Error>
        {
            Error.Validation("Name", "Name is required"),
            Error.NotFound(),
        };

        var httpResult = await AsResultTask(result).ToHttpResult();

        await Assert.That(httpResult).IsTypeOf<ProblemHttpResult>();
        var problem = (ProblemHttpResult)httpResult;
        await Assert.That(problem.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task ToHttpResult_UnmappedErrorType_MapsToInternalServerError()
    {
        ErrorOr<TestValue> result = Error.Failure();

        var httpResult = await AsResultTask(result).ToHttpResult();

        await Assert.That(httpResult).IsTypeOf<ProblemHttpResult>();
        var problem = (ProblemHttpResult)httpResult;
        await Assert.That(problem.StatusCode).IsEqualTo(StatusCodes.Status500InternalServerError);
    }
}
