using Shoplists.Domain.Models.Shoplists;
using Shoplists.Tests.Common.Builders.Shoplists;

namespace Shoplists.Domain.UnitTests.Models.Shoplists.ShoplistTests;

public sealed class ItemsTests
{
    [Test]
    public async Task Items_MutationThroughCast_Throws()
    {
        Shoplist sut = new ShoplistBuilder();
        var milk = sut.AddItem("Milk");

        var asCollection = (ICollection<ShoplistItem>)sut.Items;

        await Assert.That(() => asCollection.Remove(milk)).Throws<NotSupportedException>();
    }
}
