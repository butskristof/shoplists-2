using Shoplists.Domain.Models.Shoplists;
using Shoplists.Tests.Common.Builders.Shoplists;

namespace Shoplists.Domain.UnitTests.Models.Shoplists.ShoplistTests;

public sealed class RemoveItemTests
{
    [Test]
    public async Task RemoveItem_ItemNotFound_ThrowsInvalidOperationException()
    {
        Shoplist sut = new ShoplistBuilder();
        sut.AddItem("Milk");

        await Assert
            .That(() => sut.RemoveItem(new ShoplistItemId()))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RemoveItem_RemovesItemFromItemsCollection()
    {
        Shoplist sut = new ShoplistBuilder();
        sut.AddItem("Milk");
        var bread = sut.AddItem("Bread");
        sut.AddItem("Eggs");

        sut.RemoveItem(bread.Id);

        await Assert.That(sut.Items).DoesNotContain(bread);
    }

    [Test]
    public async Task RemoveItem_OnlyItem_LeavesItemsCollectionEmpty()
    {
        Shoplist sut = new ShoplistBuilder();
        var milk = sut.AddItem("Milk");

        sut.RemoveItem(milk.Id);

        await Assert.That(sut.Items).IsEmpty();
    }

    [Test]
    public async Task RemoveItem_FromMiddle_DecrementsFollowingPositionsAndLeavesPrecedingUnchanged()
    {
        Shoplist sut = new ShoplistBuilder();
        var milk = sut.AddItem("Milk");
        var bread = sut.AddItem("Bread");
        var eggs = sut.AddItem("Eggs");
        var cheese = sut.AddItem("Cheese");

        sut.RemoveItem(bread.Id);

        await Assert.That(milk.Position).IsEqualTo(1);
        await Assert.That(eggs.Position).IsEqualTo(2);
        await Assert.That(cheese.Position).IsEqualTo(3);
    }

    [Test]
    public async Task RemoveItem_LastItem_LeavesPrecedingPositionsUnchanged()
    {
        Shoplist sut = new ShoplistBuilder();
        var milk = sut.AddItem("Milk");
        var bread = sut.AddItem("Bread");

        sut.RemoveItem(bread.Id);

        await Assert.That(milk.Position).IsEqualTo(1);
    }
}
