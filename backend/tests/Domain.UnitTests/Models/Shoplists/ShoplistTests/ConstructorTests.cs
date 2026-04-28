using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Domain.UnitTests.Models.Shoplists.ShoplistTests;

public sealed class ConstructorTests
{
    [Test]
    public async Task Constructor_AssignsNonEmptyId()
    {
        var sut = new Shoplist { Name = "Test", OwnerId = new UserId() };

        await Assert.That(sut.Id).IsNotEqualTo(ShoplistId.Empty);
    }
}
