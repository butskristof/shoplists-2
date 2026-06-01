using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.IntegrationTests.Common;

public abstract class IntegrationTestBase
{
    [ClassDataSource<ApplicationFixture>(Shared = SharedType.PerTestSession)]
    public required ApplicationFixture Fixture { get; init; }

    // TUnit creates a fresh instance of the test class per [Test] method, so these initializers
    // run anew for every test. CurrentUserId is the ambient default actor; pass asUser to SendAsync
    // to run a single operation as a different user.
    protected UserId CurrentUserId { get; } = UserId.New();
    protected FakeTimeProvider TimeProvider { get; } = new();

    #region Dispatch

    protected ValueTask<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        UserId? asUser = null
    ) => ExecuteInScopeAsync(asUser, sender => sender.Send(command));

    protected ValueTask<TResponse> SendAsync<TResponse>(
        IQuery<TResponse> query,
        UserId? asUser = null
    ) => ExecuteInScopeAsync(asUser, sender => sender.Send(query));

    private async ValueTask<TResponse> ExecuteInScopeAsync<TResponse>(
        UserId? asUser,
        Func<ISender, ValueTask<TResponse>> dispatch
    )
    {
        // Create a fresh scope per operation, priming it with the acting user (asUser overrides the
        // ambient CurrentUserId) and the test's TimeProvider. This mirrors how a web request
        // resolves its user from its own scope (e.g. via HttpContext) rather than from shared state.
        using var scope = Fixture.CreateScopeFor(asUser ?? CurrentUserId, TimeProvider);
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await dispatch(sender);
    }

    #endregion
}
