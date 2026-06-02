using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Shoplists.Application.Common.Pipeline;

namespace Shoplists.Application.UnitTests.Common.Pipeline;

public sealed class ValidationBehaviorTests
{
    private sealed record TestMessage(string? Value = null) : IMessage;

    private sealed record TestResponse;

    private static ValidationBehavior<TestMessage, ErrorOr<TestResponse>> CreateSut(
        params IValidator<TestMessage>[] validators
    ) =>
        new(
            NullLogger<ValidationBehavior<TestMessage, ErrorOr<TestResponse>>>.Instance,
            validators
        );

    [Test]
    public async Task Handle_NoValidators_InvokesNextAndReturnsResponse()
    {
        var sut = CreateSut();
        var expected = new TestResponse();
        var nextCalled = false;

        var result = await sut.Handle(
            new TestMessage(),
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.FromResult<ErrorOr<TestResponse>>(expected);
            },
            CancellationToken.None
        );

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value).IsEqualTo(expected);
    }

    [Test]
    public async Task Handle_ValidatorsPass_InvokesNextAndReturnsResponse()
    {
        // An InlineValidator with no rules always passes.
        var sut = CreateSut(new InlineValidator<TestMessage>());
        var expected = new TestResponse();
        var nextCalled = false;

        var result = await sut.Handle(
            new TestMessage(),
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.FromResult<ErrorOr<TestResponse>>(expected);
            },
            CancellationToken.None
        );

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value).IsEqualTo(expected);
    }

    [Test]
    public async Task Handle_ValidatorsFail_ShortCircuitsWithMappedValidationErrors()
    {
        var failing = new InlineValidator<TestMessage>();
        failing.RuleFor(m => m.Value).NotEmpty();
        var sut = CreateSut(failing);
        var nextCalled = false;

        var result = await sut.Handle(
            new TestMessage(Value: null),
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.FromResult<ErrorOr<TestResponse>>(new TestResponse());
            },
            CancellationToken.None
        );

        // next is never reached...
        await Assert.That(nextCalled).IsFalse();
        // ...and the List<Error> short-circuit (the (dynamic) conversion) yields a validation error
        // mapped from the FluentValidation failure's property name.
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Validation);
        await Assert.That(result.FirstError.Code).IsEqualTo(nameof(TestMessage.Value));
    }
}
