using Shoplists.Domain.Models.Shoplists;
using Shoplists.Tests.Common.Builders.Shoplists;

namespace Shoplists.Domain.UnitTests.Models.Shoplists.ShoplistTests;

public sealed class AddItemTests
{
    [Test]
    public async Task AddItem_ToEmptyList_HasPositionOne()
    {
        Shoplist sut = new ShoplistBuilder();

        var item = sut.AddItem("Milk");

        await Assert.That(item.Position).IsEqualTo(1);
    }

    [Test]
    [Arguments(1, 2)]
    [Arguments(2, 3)]
    [Arguments(5, 6)]
    public async Task AddItem_ToNonEmptyList_HasPositionMaxPlusOne(
        int existingItems,
        int expectedPosition
    )
    {
        Shoplist sut = new ShoplistBuilder();
        for (var i = 0; i < existingItems; i++)
            sut.AddItem($"Item {i}");

        var item = sut.AddItem("New");

        await Assert.That(item.Position).IsEqualTo(expectedPosition);
    }

    [Test]
    public async Task AddItem_AppendsItemToItemsCollection()
    {
        Shoplist sut = new ShoplistBuilder();

        var item = sut.AddItem("Milk");

        await Assert.That(sut.Items).Contains(item);
    }

    [Test]
    public async Task AddItem_SetsNameAndShoplistIdOnNewItem()
    {
        Shoplist sut = new ShoplistBuilder();

        var item = sut.AddItem("Milk");

        await Assert.That(item.Name).IsEqualTo("Milk");
        await Assert.That(item.ShoplistId).IsEqualTo(sut.Id);
    }

    [Test]
    public async Task AddItem_AssignsNonEmptyIdToNewItem()
    {
        Shoplist sut = new ShoplistBuilder();

        var item = sut.AddItem("Milk");

        await Assert.That(item.Id).IsNotEqualTo(ShoplistItemId.Empty);
    }
}
