using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Shoplists.Application.Common.Pipeline;

namespace Shoplists.Application.UnitTests.Common.Pipeline;

public sealed class LoggingBehaviorTests
{
    private sealed record TestMessage : IMessage;

    private sealed record TestResponse;

    private readonly FakeLogger<LoggingBehavior<TestMessage, TestResponse>> _logger = new();

    [Test]
    public async Task Handle_WhenNextSucceeds_ReturnsResponseAndLogsStartAndCompletionAtDebug()
    {
        var expected = new TestResponse();
        var sut = new LoggingBehavior<TestMessage, TestResponse>(_logger);

        var result = await sut.Handle(
            new TestMessage(),
            (_, _) => ValueTask.FromResult(expected),
            CancellationToken.None
        );

        // Behaviour wraps the pipeline transparently...
        await Assert.That(result).IsEqualTo(expected);

        // ...and logs start + completion at Debug, with no error.
        var records = _logger.Collector.GetSnapshot();
        await Assert.That(records.Count).IsEqualTo(2);
        await Assert.That(records.All(r => r.Level == LogLevel.Debug)).IsTrue();
        await Assert.That(records.Any(r => r.Exception is not null)).IsFalse();
    }

    [Test]
    public async Task Handle_WhenNextThrows_LogsErrorWithExceptionAndRethrows()
    {
        var thrown = new InvalidOperationException("boom");
        var sut = new LoggingBehavior<TestMessage, TestResponse>(_logger);

        // The exception propagates (the behaviour does not swallow it)...
        await Assert
            .That(async () =>
                await sut.Handle(new TestMessage(), (_, _) => throw thrown, CancellationToken.None)
            )
            .Throws<InvalidOperationException>();

        // ...and it is logged at Error with the original exception attached.
        var error = _logger.Collector.GetSnapshot().Single(r => r.Level == LogLevel.Error);
        await Assert.That(error.Exception).IsSameReferenceAs(thrown);
    }
}
