using Shoplists.Tests.Common.Builders.Shoplists;
using Shoplists.Tests.Common.TestData;

namespace Shoplists.Domain.UnitTests.Models.Shoplists.ShoplistItemTests;

public sealed class NameTests
{
    [Test]
    [NullEmptyOrWhitespaceStrings]
    public async Task Name_NullOrWhitespace_ThrowsArgumentException(string? name)
    {
        var shoplist = new ShoplistBuilder().Build();
        var item = shoplist.AddItem("Milk");

        await Assert.That(() => item.Name = name!).Throws<ArgumentException>();
    }

    [Test]
    public async Task Name_WithSurroundingWhitespace_StoresTrimmedValue()
    {
        var shoplist = new ShoplistBuilder().Build();
        var item = shoplist.AddItem("Milk");

        item.Name = "  Cheese  ";

        await Assert.That(item.Name).IsEqualTo("Cheese");
    }
}
