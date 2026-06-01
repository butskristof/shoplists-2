using ErrorOr;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Application.Features.Shoplists.Items;
using Shoplists.Application.IntegrationTests.Common;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.IntegrationTests.Features.Shoplists.Items;

public sealed class CreateShoplistItemTests : IntegrationTestBase
{
    [Test]
    public async Task ValidRequest_CreatesItem()
    {
        var shoplistId = await CreateShoplistAsync("Groceries");

        var result = await SendAsync(new CreateShoplistItem.Request(shoplistId, "Milk"));

        await Assert.That(result.IsError).IsFalse();
        var getResult = await SendAsync(new GetShoplist.Request(shoplistId));
        await Assert.That(getResult.Value.Items.Count).IsEqualTo(1);
        var item = getResult.Value.Items[0];
        await Assert.That(item.Id).IsEqualTo(result.Value.Id);
        await Assert.That(item.Name).IsEqualTo("Milk");
        await Assert.That(item.IsFulfilled).IsFalse();
        await Assert.That(item.Position).IsEqualTo(1);
    }

    [Test]
    public async Task AssignsSequentialPositions()
    {
        var shoplistId = await CreateShoplistAsync("Groceries");

        await SendAsync(new CreateShoplistItem.Request(shoplistId, "Milk"));
        await SendAsync(new CreateShoplistItem.Request(shoplistId, "Bread"));
        await SendAsync(new CreateShoplistItem.Request(shoplistId, "Eggs"));

        var items = (await SendAsync(new GetShoplist.Request(shoplistId))).Value.Items;
        await Assert.That(items.Count).IsEqualTo(3);
        await Assert.That(items[0].Name).IsEqualTo("Milk");
        await Assert.That(items[0].Position).IsEqualTo(1);
        await Assert.That(items[1].Name).IsEqualTo("Bread");
        await Assert.That(items[1].Position).IsEqualTo(2);
        await Assert.That(items[2].Name).IsEqualTo("Eggs");
        await Assert.That(items[2].Position).IsEqualTo(3);
    }

    [Test]
    public async Task UnknownShoplist_ReturnsNotFound()
    {
        var result = await SendAsync(new CreateShoplistItem.Request(ShoplistId.New(), "Milk"));

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task OtherUsersShoplist_ReturnsNotFound()
    {
        var otherUser = UserId.New();
        var shoplistId = await CreateShoplistAsync("Their list", asUser: otherUser);

        var result = await SendAsync(new CreateShoplistItem.Request(shoplistId, "Milk"));

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }
}
