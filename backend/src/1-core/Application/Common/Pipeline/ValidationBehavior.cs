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
            return await next(message, cancellationToken);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(message, cancellationToken))
        );

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (errors.Count == 0)
            return await next(message, cancellationToken);

        // All handlers return ErrorOr<T>, which has an implicit conversion from List<Error>.
        // The Mediator source generator fills TResponse as the full ErrorOr<T> type, preventing
        // a partially-closed generic constraint (it would double-wrap as ErrorOr<ErrorOr<T>>).
        // Dynamic dispatch is used here to invoke the implicit conversion at runtime.
        // This is a known pragmatic trade-off — the conversion is a stable part of ErrorOr's API.
        return (dynamic)errors;
    }
}
