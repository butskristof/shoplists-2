using ErrorOr;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Application.Features.Shoplists.Items;
using Shoplists.Application.IntegrationTests.Common;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.IntegrationTests.Features.Shoplists.Items;

public sealed class UpdateShoplistItemPositionTests : IntegrationTestBase
{
    [Test]
    public async Task MovingItemDown_PersistsReorderedPositions()
    {
        var shoplistId = await CreateShoplistAsync("Groceries");
        var itemAId = await AddItemAsync(shoplistId, "A");
        await AddItemAsync(shoplistId, "B");
        await AddItemAsync(shoplistId, "C");

        var result = await SendAsync(
            new UpdateShoplistItemPosition.Request(shoplistId, itemAId, 3)
        );

        await Assert.That(result.IsError).IsFalse();
        var items = (await SendAsync(new GetShoplist.Request(shoplistId))).Value.Items;
        await Assert.That(items[0].Name).IsEqualTo("B");
        await Assert.That(items[0].Position).IsEqualTo(1);
        await Assert.That(items[1].Name).IsEqualTo("C");
        await Assert.That(items[1].Position).IsEqualTo(2);
        await Assert.That(items[2].Name).IsEqualTo("A");
        await Assert.That(items[2].Position).IsEqualTo(3);
    }

    [Test]
    public async Task MovingItemUp_PersistsReorderedPositions()
    {
        var shoplistId = await CreateShoplistAsync("Groceries", ["A", "B"]);
        var itemCId = await AddItemAsync(shoplistId, "C");

        var result = await SendAsync(
            new UpdateShoplistItemPosition.Request(shoplistId, itemCId, 1)
        );

        await Assert.That(result.IsError).IsFalse();
        var items = (await SendAsync(new GetShoplist.Request(shoplistId))).Value.Items;
        await Assert.That(items[0].Name).IsEqualTo("C");
        await Assert.That(items[0].Position).IsEqualTo(1);
        await Assert.That(items[1].Name).IsEqualTo("A");
        await Assert.That(items[1].Position).IsEqualTo(2);
        await Assert.That(items[2].Name).IsEqualTo("B");
        await Assert.That(items[2].Position).IsEqualTo(3);
    }

    [Test]
    public async Task PositionOutOfRange_ReturnsValidation()
    {
        // Position >= 1 clears the validator, so this exercises the handler's own out-of-range
        // guard (MoveItem returning false) rather than the FluentValidation rule.
        var shoplistId = await CreateShoplistAsync("Groceries");
        var itemAId = await AddItemAsync(shoplistId, "A");
        await AddItemAsync(shoplistId, "B");

        var result = await SendAsync(
            new UpdateShoplistItemPosition.Request(shoplistId, itemAId, 5)
        );

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Validation);
    }

    [Test]
    public async Task UnknownShoplist_ReturnsNotFound()
    {
        var result = await SendAsync(
            new UpdateShoplistItemPosition.Request(
                ShoplistId.New(),
                ShoplistItemId.New(),
                Position: 1
            )
        );

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task UnknownItem_ReturnsNotFound()
    {
        var shoplistId = await CreateShoplistAsync("Groceries");

        var result = await SendAsync(
            new UpdateShoplistItemPosition.Request(shoplistId, ShoplistItemId.New(), Position: 1)
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

        var result = await SendAsync(
            new UpdateShoplistItemPosition.Request(shoplistId, itemId, Position: 1)
        );

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }
}
