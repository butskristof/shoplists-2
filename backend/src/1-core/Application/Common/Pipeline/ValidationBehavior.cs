using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Shoplists.Application.Common.Pipeline;

/// <summary>
/// Represents a validation pipeline behavior used in the Mediator chain to enforce validation
/// on incoming messages before they reach their respective handlers. This behavior utilizes
/// FluentValidation to validate messages and produce a collection of validation errors if any
/// rules are violated.
/// </summary>
/// <typeparam name="TMessage">The type of the message being processed in the pipeline.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the handler after processing the message.</typeparam>
/// <remarks>
/// This behavior intercepts the message-handling pipeline and applies validation using the provided
/// validators. If no validation errors are found, the message is forwarded to the next pipeline
/// behavior or the final handler. If validation errors exist, the behavior short-circuits the pipeline
/// and returns the collection of errors as the response.
/// </remarks>
internal sealed class ValidationBehavior<TMessage, TResponse>(
    ILogger<ValidationBehavior<TMessage, TResponse>> logger,
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
        var messageType = typeof(TMessage).Name;
        var validatorCount = validators.Count();

        if (validatorCount == 0)
        {
            logger.LogDebug(
                "No validators registered for {MessageType}, skipping validation",
                messageType
            );
            return await next(message, cancellationToken);
        }

        logger.LogDebug(
            "Running {ValidatorCount} validator(s) for {MessageType}",
            validatorCount,
            messageType
        );

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(message, cancellationToken))
        );

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (errors.Count == 0)
        {
            logger.LogDebug("Validation passed for {MessageType}", messageType);
            return await next(message, cancellationToken);
        }

        logger.LogDebug(
            "Validation failed for {MessageType} with {ErrorCount} error(s)",
            messageType,
            errors.Count
        );

        // All handlers return ErrorOr<T>, which has an implicit conversion from List<Error>.
        // The Mediator source generator fills TResponse as the full ErrorOr<T> type, preventing
        // a partially-closed generic constraint (it would double-wrap as ErrorOr<ErrorOr<T>>).
        // Dynamic dispatch is used here to invoke the implicit conversion at runtime.
        // This is a known pragmatic trade-off — the conversion is a stable part of ErrorOr's API.
        return (dynamic)errors;
    }
}
