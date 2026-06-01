using Shoplists.Application.Features.Shoplists;
using Shoplists.Application.Features.Shoplists.Items;
using Shoplists.Application.IntegrationTests.Common;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.IntegrationTests.Features.Shoplists;

public sealed class GetShoplistsTests : IntegrationTestBase
{
    [Test]
    public async Task NoShoplists_ReturnsEmpty()
    {
        var result = await SendAsync(new GetShoplists.Request());

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value).IsEmpty();
    }

    [Test]
    public async Task ReturnsOnlyCurrentUsersShoplists()
    {
        var createResult = await SendAsync(new CreateShoplist.Request("Groceries"));
        var shoplistId = createResult.Value.Id;
        await SendAsync(new CreateShoplist.Request("Theirs"), asUser: UserId.New());

        var result = await SendAsync(new GetShoplists.Request());

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Count).IsEqualTo(1);
        await Assert.That(result.Value[0].Id).IsEqualTo(shoplistId);
    }

    [Test]
    public async Task ReturnsItemCounts_TotalAndFulfilled()
    {
        var createResult = await SendAsync(new CreateShoplist.Request("Groceries"));
        var shoplistId = createResult.Value.Id;
        var milk = await SendAsync(new CreateShoplistItem.Request(shoplistId, "Milk"));
        await SendAsync(new CreateShoplistItem.Request(shoplistId, "Bread"));
        await SendAsync(new CreateShoplistItem.Request(shoplistId, "Eggs"));
        await SendAsync(
            new UpdateShoplistItemFulfilled.Request(shoplistId, milk.Value.Id, IsFulfilled: true)
        );

        var result = await SendAsync(new GetShoplists.Request());

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Count).IsEqualTo(1);
        await Assert.That(result.Value[0].Items.Total).IsEqualTo(3);
        await Assert.That(result.Value[0].Items.Fulfilled).IsEqualTo(1);
    }

    [Test]
    public async Task ShoplistWithoutItems_HasZeroItemCounts()
    {
        await SendAsync(new CreateShoplist.Request("Empty"));

        var result = await SendAsync(new GetShoplists.Request());

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value[0].Items.Total).IsEqualTo(0);
        await Assert.That(result.Value[0].Items.Fulfilled).IsEqualTo(0);
    }
}
