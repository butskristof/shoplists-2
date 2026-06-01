using ErrorOr;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Application.Features.Shoplists.Items;
using Shoplists.Application.IntegrationTests.Common;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.IntegrationTests.Features.Shoplists.Items;

public sealed class UpdateShoplistItemFulfilledTests : IntegrationTestBase
{
    [Test]
    public async Task ValidRequest_MarksItemFulfilled()
    {
        var createResult = await SendAsync(new CreateShoplist.Request("Groceries"));
        var shoplistId = createResult.Value.Id;
        var itemResult = await SendAsync(new CreateShoplistItem.Request(shoplistId, "Milk"));
        var itemId = itemResult.Value.Id;

        var result = await SendAsync(
            new UpdateShoplistItemFulfilled.Request(shoplistId, itemId, IsFulfilled: true)
        );

        await Assert.That(result.IsError).IsFalse();
        var getResult = await SendAsync(new GetShoplist.Request(shoplistId));
        await Assert.That(getResult.Value.Items[0].IsFulfilled).IsTrue();
    }

    [Test]
    public async Task ValidRequest_MarksItemUnfulfilled()
    {
        var createResult = await SendAsync(new CreateShoplist.Request("Groceries"));
        var shoplistId = createResult.Value.Id;
        var itemResult = await SendAsync(new CreateShoplistItem.Request(shoplistId, "Milk"));
        var itemId = itemResult.Value.Id;
        await SendAsync(
            new UpdateShoplistItemFulfilled.Request(shoplistId, itemId, IsFulfilled: true)
        );

        var result = await SendAsync(
            new UpdateShoplistItemFulfilled.Request(shoplistId, itemId, IsFulfilled: false)
        );

        await Assert.That(result.IsError).IsFalse();
        var getResult = await SendAsync(new GetShoplist.Request(shoplistId));
        await Assert.That(getResult.Value.Items[0].IsFulfilled).IsFalse();
    }

    [Test]
    public async Task UnknownShoplist_ReturnsNotFound()
    {
        var result = await SendAsync(
            new UpdateShoplistItemFulfilled.Request(
                ShoplistId.New(),
                ShoplistItemId.New(),
                IsFulfilled: true
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
            new UpdateShoplistItemFulfilled.Request(
                createResult.Value.Id,
                ShoplistItemId.New(),
                IsFulfilled: true
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
            new UpdateShoplistItemFulfilled.Request(
                createResult.Value.Id,
                itemResult.Value.Id,
                IsFulfilled: true
            )
        );

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }
}
