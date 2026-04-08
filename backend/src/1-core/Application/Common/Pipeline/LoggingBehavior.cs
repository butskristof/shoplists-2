using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Shoplists.Application.Common.Pipeline;

internal sealed class LoggingBehavior<TMessage, TResponse>(
    ILogger<LoggingBehavior<TMessage, TResponse>> logger
) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var handlerName = typeof(TMessage).DeclaringType?.Name ?? typeof(TMessage).Name;

        logger.LogDebug("Handling {Handler}", handlerName);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next(message, cancellationToken);
            stopwatch.Stop();

            logger.LogDebug(
                "Handled {Handler} in {ElapsedMilliseconds}ms",
                handlerName,
                stopwatch.ElapsedMilliseconds
            );

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(
                ex,
                "Failed handling {Handler} after {ElapsedMilliseconds}ms",
                handlerName,
                stopwatch.ElapsedMilliseconds
            );

            throw;
        }
    }
}
