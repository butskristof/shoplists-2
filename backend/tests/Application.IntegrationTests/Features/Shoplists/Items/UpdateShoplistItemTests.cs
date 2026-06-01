using ErrorOr;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Application.Features.Shoplists.Items;
using Shoplists.Application.IntegrationTests.Common;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.IntegrationTests.Features.Shoplists.Items;

public sealed class UpdateShoplistItemTests : IntegrationTestBase
{
    [Test]
    public async Task ValidRequest_UpdatesName()
    {
        var createResult = await SendAsync(new CreateShoplist.Request("Groceries"));
        var shoplistId = createResult.Value.Id;
        var itemResult = await SendAsync(new CreateShoplistItem.Request(shoplistId, "Milk"));
        var itemId = itemResult.Value.Id;

        var result = await SendAsync(
            new UpdateShoplistItem.Request(shoplistId, itemId, "Almond milk")
        );

        await Assert.That(result.IsError).IsFalse();
        var getResult = await SendAsync(new GetShoplist.Request(shoplistId));
        await Assert.That(getResult.Value.Items[0].Name).IsEqualTo("Almond milk");
    }

    [Test]
    public async Task UnknownShoplist_ReturnsNotFound()
    {
        var result = await SendAsync(
            new UpdateShoplistItem.Request(ShoplistId.New(), ShoplistItemId.New(), "Milk")
        );

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task UnknownItem_ReturnsNotFound()
    {
        var createResult = await SendAsync(new CreateShoplist.Request("Groceries"));

        var result = await SendAsync(
            new UpdateShoplistItem.Request(createResult.Value.Id, ShoplistItemId.New(), "Milk")
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
            new UpdateShoplistItem.Request(createResult.Value.Id, itemResult.Value.Id, "Hacked")
        );

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }
}
