using ErrorOr;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Application.Features.Shoplists.Items;
using Shoplists.Application.IntegrationTests.Common;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.IntegrationTests.Features.Shoplists.Items;

public sealed class DeleteShoplistItemTests : IntegrationTestBase
{
    [Test]
    public async Task ValidRequest_DeletesItem()
    {
        var shoplistId = await CreateShoplistAsync("Groceries");
        var itemId = await AddItemAsync(shoplistId, "Milk");

        var result = await SendAsync(new DeleteShoplistItem.Request(shoplistId, itemId));

        await Assert.That(result.IsError).IsFalse();
        var getResult = await SendAsync(new GetShoplist.Request(shoplistId));
        await Assert.That(getResult.Value.Items).IsEmpty();
    }

    [Test]
    public async Task DeletingMiddleItem_CompactsFollowingPositions()
    {
        var shoplistId = await CreateShoplistAsync("Groceries");
        await AddItemAsync(shoplistId, "A");
        var itemBId = await AddItemAsync(shoplistId, "B");
        await AddItemAsync(shoplistId, "C");

        var result = await SendAsync(new DeleteShoplistItem.Request(shoplistId, itemBId));

        await Assert.That(result.IsError).IsFalse();
        var items = (await SendAsync(new GetShoplist.Request(shoplistId))).Value.Items;
        await Assert.That(items.Count).IsEqualTo(2);
        await Assert.That(items[0].Name).IsEqualTo("A");
        await Assert.That(items[0].Position).IsEqualTo(1);
        await Assert.That(items[1].Name).IsEqualTo("C");
        await Assert.That(items[1].Position).IsEqualTo(2);
    }

    [Test]
    public async Task UnknownShoplist_ReturnsNotFound()
    {
        var result = await SendAsync(
            new DeleteShoplistItem.Request(ShoplistId.New(), ShoplistItemId.New())
        );

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task UnknownItem_ReturnsNotFound()
    {
        var shoplistId = await CreateShoplistAsync("Groceries");

        var result = await SendAsync(
            new DeleteShoplistItem.Request(shoplistId, ShoplistItemId.New())
        );

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task OtherUsersShoplist_ReturnsNotFound()
    {
        var otherUser = UserId.New();
        var shoplistId = await CreateShoplistAsync("Their list", asUser: otherUser);
        var itemId = await AddItemAsync(shoplistId, "Milk", asUser: otherUser);

        var result = await SendAsync(new DeleteShoplistItem.Request(shoplistId, itemId));

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }
}
