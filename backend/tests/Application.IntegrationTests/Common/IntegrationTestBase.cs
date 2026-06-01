using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Application.Features.Shoplists.Items;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;
using Shoplists.Persistence;
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

    #region Arrange

    // Handler-based arrange: build preconditions through the real mediator path (no second
    // construction route to drift from production) and assert success once, centrally, so test
    // bodies stay focused on the behaviour under test. For state the handler vocabulary can't
    // express (out-of-order positions, raw rows), use the Direct database access escape hatch below.

    protected async ValueTask<ShoplistId> CreateShoplistAsync(string name, UserId? asUser = null)
    {
        var result = await SendAsync(new CreateShoplist.Request(name), asUser);
        await Assert.That(result.IsError).IsFalse();
        return result.Value.Id;
    }

    protected async ValueTask<ShoplistItemId> AddItemAsync(
        ShoplistId shoplistId,
        string name,
        UserId? asUser = null
    )
    {
        var result = await SendAsync(new CreateShoplistItem.Request(shoplistId, name), asUser);
        await Assert.That(result.IsError).IsFalse();
        return result.Value.Id;
    }

    protected async ValueTask<ShoplistId> CreateShoplistAsync(
        string name,
        IEnumerable<string> itemNames,
        UserId? asUser = null
    )
    {
        var shoplistId = await CreateShoplistAsync(name, asUser);
        foreach (var itemName in itemNames)
            await AddItemAsync(shoplistId, itemName, asUser);
        return shoplistId;
    }

    #endregion

    #region Direct database access

    // Escape hatch for arrange/assert that the mediator surface cannot express: seeding arbitrary
    // state (other users' data, specific positions) and verifying raw rows (e.g. proving cascade
    // deletes physically removed ShoplistItems, which no handler can observe). Each call gets a
    // fresh scope + DbContext, so reads reflect committed state with no change-tracker bleed.
    // Access is UNFILTERED: context.Set<T>() / context.Shoplists bypass the OwnerId filter that
    // lives only in CurrentUserShoplists(); the asUser priming just satisfies the DbContext ctor.

    private protected async ValueTask ExecuteDbAsync(
        Func<AppDbContext, ValueTask> action,
        UserId? asUser = null
    )
    {
        using var scope = Fixture.CreateScopeFor(asUser ?? CurrentUserId, TimeProvider);
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(dbContext);
    }

    private protected async ValueTask<TResult> ExecuteDbAsync<TResult>(
        Func<AppDbContext, ValueTask<TResult>> action,
        UserId? asUser = null
    )
    {
        using var scope = Fixture.CreateScopeFor(asUser ?? CurrentUserId, TimeProvider);
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(dbContext);
    }

    #endregion
}
