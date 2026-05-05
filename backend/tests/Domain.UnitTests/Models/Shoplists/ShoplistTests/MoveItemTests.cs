using Shoplists.Domain.Models.Shoplists;
using Shoplists.Testing.Common.Builders.Shoplists;

namespace Shoplists.Domain.UnitTests.Models.Shoplists.ShoplistTests;

public sealed class MoveItemTests
{
    [Test]
    public async Task MoveItem_ItemNotFound_ThrowsInvalidOperationException()
    {
        Shoplist sut = new ShoplistBuilder();
        sut.AddItem("Milk");

        await Assert
            .That(() => sut.MoveItem(new ShoplistItemId(), 1))
            .Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task MoveItem_NewPositionLessThanOne_ReturnsFalse(int newPosition)
    {
        Shoplist sut = new ShoplistBuilder();
        var milk = sut.AddItem("Milk");

        var result = sut.MoveItem(milk.Id, newPosition);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task MoveItem_NewPositionGreaterThanItemCount_ReturnsFalse()
    {
        Shoplist sut = new ShoplistBuilder();
        var milk = sut.AddItem("Milk");
        sut.AddItem("Bread");

        var result = sut.MoveItem(milk.Id, 3);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task MoveItem_InvalidNewPosition_LeavesPositionsUnchanged()
    {
        Shoplist sut = new ShoplistBuilder();
        var milk = sut.AddItem("Milk");
        var bread = sut.AddItem("Bread");

        sut.MoveItem(milk.Id, 5);

        await Assert.That(milk.Position).IsEqualTo(1);
        await Assert.That(bread.Position).IsEqualTo(2);
    }

    [Test]
    public async Task MoveItem_SamePosition_ReturnsTrueAndLeavesPositionsUnchanged()
    {
        Shoplist sut = new ShoplistBuilder();
        var milk = sut.AddItem("Milk");
        var bread = sut.AddItem("Bread");

        var result = sut.MoveItem(milk.Id, 1);

        await Assert.That(result).IsTrue();
        await Assert.That(milk.Position).IsEqualTo(1);
        await Assert.That(bread.Position).IsEqualTo(2);
    }

    [Test]
    public async Task MoveItem_Down_PlacesItemAtNewPositionAndShiftsInterveningItemsUp()
    {
        Shoplist sut = new ShoplistBuilder();
        var milk = sut.AddItem("Milk");
        var bread = sut.AddItem("Bread");
        var eggs = sut.AddItem("Eggs");
        var cheese = sut.AddItem("Cheese");

        var result = sut.MoveItem(milk.Id, 3);

        await Assert.That(result).IsTrue();
        await Assert.That(milk.Position).IsEqualTo(3);
        await Assert.That(bread.Position).IsEqualTo(1);
        await Assert.That(eggs.Position).IsEqualTo(2);
        await Assert.That(cheese.Position).IsEqualTo(4);
    }

    [Test]
    public async Task MoveItem_Up_PlacesItemAtNewPositionAndShiftsInterveningItemsDown()
    {
        Shoplist sut = new ShoplistBuilder();
        var milk = sut.AddItem("Milk");
        var bread = sut.AddItem("Bread");
        var eggs = sut.AddItem("Eggs");
        var cheese = sut.AddItem("Cheese");

        var result = sut.MoveItem(cheese.Id, 2);

        await Assert.That(result).IsTrue();
        await Assert.That(cheese.Position).IsEqualTo(2);
        await Assert.That(milk.Position).IsEqualTo(1);
        await Assert.That(bread.Position).IsEqualTo(3);
        await Assert.That(eggs.Position).IsEqualTo(4);
    }
}
