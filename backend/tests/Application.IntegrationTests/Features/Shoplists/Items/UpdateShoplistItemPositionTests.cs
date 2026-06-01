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
        var createResult = await SendAsync(new CreateShoplist.Request("Groceries"));
        var shoplistId = createResult.Value.Id;
        var a = await SendAsync(new CreateShoplistItem.Request(shoplistId, "A"));
        await SendAsync(new CreateShoplistItem.Request(shoplistId, "B"));
        await SendAsync(new CreateShoplistItem.Request(shoplistId, "C"));

        var result = await SendAsync(
            new UpdateShoplistItemPosition.Request(shoplistId, a.Value.Id, 3)
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
        var createResult = await SendAsync(new CreateShoplist.Request("Groceries"));
        var shoplistId = createResult.Value.Id;
        await SendAsync(new CreateShoplistItem.Request(shoplistId, "A"));
        await SendAsync(new CreateShoplistItem.Request(shoplistId, "B"));
        var c = await SendAsync(new CreateShoplistItem.Request(shoplistId, "C"));

        var result = await SendAsync(
            new UpdateShoplistItemPosition.Request(shoplistId, c.Value.Id, 1)
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
        var createResult = await SendAsync(new CreateShoplist.Request("Groceries"));
        var shoplistId = createResult.Value.Id;
        var a = await SendAsync(new CreateShoplistItem.Request(shoplistId, "A"));
        await SendAsync(new CreateShoplistItem.Request(shoplistId, "B"));

        var result = await SendAsync(
            new UpdateShoplistItemPosition.Request(shoplistId, a.Value.Id, 5)
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
        var createResult = await SendAsync(new CreateShoplist.Request("Groceries"));

        var result = await SendAsync(
            new UpdateShoplistItemPosition.Request(
                createResult.Value.Id,
                ShoplistItemId.New(),
                Position: 1
            )
        );

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task OtherUsersShoplist_ReturnsNotFound()
    {
        var otherUser = UserId.New();
        var createResult = await SendAsync(
            new CreateShoplist.Request("Their list"),
            asUser: otherUser
        );
        var itemResult = await SendAsync(
            new CreateShoplistItem.Request(createResult.Value.Id, "Milk"),
            asUser: otherUser
        );

        var result = await SendAsync(
            new UpdateShoplistItemPosition.Request(
                createResult.Value.Id,
                itemResult.Value.Id,
                Position: 1
            )
        );

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }
}
