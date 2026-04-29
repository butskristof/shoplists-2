using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Domain.UnitTests.Models.Shoplists.ShoplistTests;

public sealed class ConstructorTests
{
    [Test]
    public async Task Constructor_AssignsNonEmptyId()
    {
        var sut = new Shoplist { Name = "Test", OwnerId = new UserId() };

        await Assert.That(sut.Id).IsNotEqualTo(ShoplistId.Empty);
    }

    [Test]
    [NullEmptyOrWhitespaceStrings]
    public async Task Constructor_NullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        await Assert
            .That(() => new Shoplist { Name = name!, OwnerId = new UserId() })
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_NameWithSurroundingWhitespace_StoresTrimmedName()
    {
        var sut = new Shoplist { Name = "  Test  ", OwnerId = new UserId() };

        await Assert.That(sut.Name).IsEqualTo("Test");
    }
}
