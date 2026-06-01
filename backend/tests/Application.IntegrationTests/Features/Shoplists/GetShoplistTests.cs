using ErrorOr;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Application.IntegrationTests.Common;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.Builders.Shoplists;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.IntegrationTests.Features.Shoplists;

public sealed class GetShoplistTests : IntegrationTestBase
{
    [Test]
    public async Task ValidRequest_ReturnsShoplistWithItemsOrderedByPosition()
    {
        // Seed directly so the stored row order differs from position order (Eggs moved to the
        // front): this proves the handler's OrderBy(Position) projection actually sorts, rather
        // than relying on insertion order happening to match.
        var shoplist = new ShoplistBuilder().WithOwnerId(CurrentUserId).Build();
        shoplist.AddItem("Milk");
        shoplist.AddItem("Bread");
        var eggs = shoplist.AddItem("Eggs");
        shoplist.MoveItem(eggs.Id, 1);
        await ExecuteDbAsync(async db =>
        {
            db.Shoplists.Add(shoplist);
            await db.SaveChangesAsync();
        });

        var result = await SendAsync(new GetShoplist.Request(shoplist.Id));

        await Assert.That(result.IsError).IsFalse();
        var value = result.Value;
        await Assert.That(value.Id).IsEqualTo(shoplist.Id);
        await Assert.That(value.Name).IsEqualTo("Test Shoplist");
        await Assert.That(value.Items.Count).IsEqualTo(3);
        await Assert.That(value.Items[0].Name).IsEqualTo("Eggs");
        await Assert.That(value.Items[0].Position).IsEqualTo(1);
        await Assert.That(value.Items[1].Name).IsEqualTo("Milk");
        await Assert.That(value.Items[1].Position).IsEqualTo(2);
        await Assert.That(value.Items[2].Name).IsEqualTo("Bread");
        await Assert.That(value.Items[2].Position).IsEqualTo(3);
    }

    [Test]
    public async Task ShoplistWithoutItems_ReturnsEmptyItemsCollection()
    {
        var createResult = await SendAsync(new CreateShoplist.Request("Groceries"));

        var result = await SendAsync(new GetShoplist.Request(createResult.Value.Id));

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Items).IsEmpty();
    }

    [Test]
    public async Task UnknownId_ReturnsNotFound()
    {
        var result = await SendAsync(new GetShoplist.Request(ShoplistId.New()));

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

        var result = await SendAsync(new GetShoplist.Request(createResult.Value.Id));

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }
}
