using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Application.IntegrationTests.Common;

public abstract class IntegrationTestBase
{
    [ClassDataSource<ApplicationFixture>(Shared = SharedType.PerTestSession)]
    public required ApplicationFixture Fixture { get; init; }

    // TUnit creates a fresh instance of the test class per [Test] method, so these initializers
    // run anew for every test. No reset hook required.
    public UserId CurrentUserId { get; private set; } = NewTestUserId();
    public FakeTimeProvider TimeProvider { get; } = new();

    public void SetUserId(UserId userId) => CurrentUserId = userId;

    public void SetUtcNow(DateTimeOffset value) => TimeProvider.SetUtcNow(value);

    public static UserId NewTestUserId() => new(Guid.NewGuid().ToString());

    public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request) =>
        ExecuteInScopeAsync(sender => sender.Send(request));

    public ValueTask<TResponse> SendAsync<TResponse>(ICommand<TResponse> command) =>
        ExecuteInScopeAsync(sender => sender.Send(command));

    public ValueTask<TResponse> SendAsync<TResponse>(IQuery<TResponse> query) =>
        ExecuteInScopeAsync(sender => sender.Send(query));

    private async ValueTask<TResponse> ExecuteInScopeAsync<TResponse>(
        Func<ISender, ValueTask<TResponse>> dispatch
    )
    {
        using var scope = Fixture.CreateScopeFor(CurrentUserId, TimeProvider);
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await dispatch(sender);
    }
}
