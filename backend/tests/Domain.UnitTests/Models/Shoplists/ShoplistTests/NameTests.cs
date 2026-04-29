using Shoplists.Domain.Models.Shoplists;
using Shoplists.Tests.Common.Builders.Shoplists;
using Shoplists.Tests.Common.TestData;

namespace Shoplists.Domain.UnitTests.Models.Shoplists.ShoplistTests;

public sealed class NameTests
{
    [Test]
    [NullEmptyOrWhitespaceStrings]
    public async Task Name_NullOrWhitespace_ThrowsArgumentException(string? name)
    {
        Shoplist sut = new ShoplistBuilder();

        await Assert.That(() => sut.Name = name!).Throws<ArgumentException>();
    }

    [Test]
    public async Task Name_WithSurroundingWhitespace_StoresTrimmedValue()
    {
        Shoplist sut = new ShoplistBuilder();

        sut.Name = "  Renamed  ";

        await Assert.That(sut.Name).IsEqualTo("Renamed");
    }
}
