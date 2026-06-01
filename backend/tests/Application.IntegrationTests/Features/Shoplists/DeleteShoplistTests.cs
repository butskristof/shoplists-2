using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Application.IntegrationTests.Common;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.IntegrationTests.Features.Shoplists;

public sealed class DeleteShoplistTests : IntegrationTestBase
{
    [Test]
    public async Task ValidRequest_DeletesShoplist()
    {
        var shoplistId = await CreateShoplistAsync("Groceries");

        var result = await SendAsync(new DeleteShoplist.Request(shoplistId));

        await Assert.That(result.IsError).IsFalse();
        var listResult = await SendAsync(new GetShoplists.Request());
        await Assert.That(listResult.Value.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ShoplistWithItems_CascadeDeletesItems()
    {
        var shoplistId = await CreateShoplistAsync("Groceries", ["Milk", "Bread"]);

        var result = await SendAsync(new DeleteShoplist.Request(shoplistId));

        await Assert.That(result.IsError).IsFalse();
        // No handler can observe orphaned items, so go straight to the items table to prove the
        // configured cascade physically removed them rather than leaving them parentless.
        var remainingItems = await ExecuteDbAsync(db => new ValueTask<int>(
            db.Set<ShoplistItem>().CountAsync(i => i.ShoplistId == shoplistId)
        ));
        await Assert.That(remainingItems).IsEqualTo(0);
    }

    [Test]
    public async Task UnknownId_ReturnsNotFound()
    {
        var result = await SendAsync(new DeleteShoplist.Request(ShoplistId.New()));

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task OtherUsersShoplist_ReturnsNotFound_AndLeavesItIntact()
    {
        var otherUser = UserId.New();
        var shoplistId = await CreateShoplistAsync("Their list", asUser: otherUser);

        var result = await SendAsync(new DeleteShoplist.Request(shoplistId));

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
        var otherUserLists = await SendAsync(new GetShoplists.Request(), asUser: otherUser);
        await Assert.That(otherUserLists.Value.Count).IsEqualTo(1);
    }
}
