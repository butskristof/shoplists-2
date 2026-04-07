using ErrorOr;
using FluentValidation;
using Mediator;

namespace Shoplists.Application.Common.Pipeline;

internal sealed class ValidationBehavior<TMessage, TResponse>(
    IEnumerable<IValidator<TMessage>> validators
) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (!validators.Any())
            return await next(message, cancellationToken).ConfigureAwait(false);

        var validationResults = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(message, cancellationToken))
            )
            .ConfigureAwait(false);

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (errors.Count == 0)
            return await next(message, cancellationToken).ConfigureAwait(false);

        // ErrorOr<T> has an implicit conversion from List<Error>.
        // We need to get the inner T from ErrorOr<TResponse> to construct the error result.
        // Since all our handlers return ErrorOr<T>, TResponse is ErrorOr<T>.
        // ErrorOr<T> supports implicit conversion from List<Error>.
        return (dynamic)errors;
    }
}
