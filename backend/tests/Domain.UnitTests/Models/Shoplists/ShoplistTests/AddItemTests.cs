using Shoplists.Domain.Models.Shoplists;
using Shoplists.Tests.Common.Builders.Shoplists;
using Shoplists.Tests.Common.TestData;

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

    [Test]
    [NullEmptyOrWhitespaceStrings]
    public async Task AddItem_NullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        Shoplist sut = new ShoplistBuilder();

        await Assert.That(() => sut.AddItem(name!)).Throws<ArgumentException>();
    }

    [Test]
    public async Task AddItem_NameWithSurroundingWhitespace_StoresTrimmedName()
    {
        Shoplist sut = new ShoplistBuilder();

        var item = sut.AddItem("  Milk  ");

        await Assert.That(item.Name).IsEqualTo("Milk");
    }

    [Test]
    public async Task AddItem_NewItem_IsNotFulfilledByDefault()
    {
        Shoplist sut = new ShoplistBuilder();

        var item = sut.AddItem("Milk");

        await Assert.That(item.IsFulfilled).IsFalse();
    }

    [Test]
    [Arguments(1)]
    [Arguments(3)]
    [Arguments(7)]
    public async Task AddItem_AssignsContiguousPositionsFromOne(int count)
    {
        Shoplist sut = new ShoplistBuilder();

        for (var i = 0; i < count; i++)
            sut.AddItem($"Item {i}");

        var positions = sut.Items.Select(i => i.Position);

        await Assert.That(positions).IsEquivalentTo(Enumerable.Range(1, count));
    }
}
