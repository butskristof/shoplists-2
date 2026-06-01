using ErrorOr;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Application.IntegrationTests.Common;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.IntegrationTests.Features.Shoplists;

public sealed class CreateShoplistTests : IntegrationTestBase
{
    [Test]
    public async Task ValidRequest_PersistsShoplist_AndReturnsId()
    {
        var request = new CreateShoplist.Request("Groceries");

        var result = await SendAsync(request);

        await Assert.That(result.IsError).IsFalse();
        var shoplistsResult = await SendAsync(new GetShoplists.Request());
        await Assert.That(shoplistsResult.IsError).IsFalse();
        var shoplists = shoplistsResult.Value;
        await Assert.That(shoplists.Count).IsEqualTo(1);
        await Assert.That(shoplists[0].Id).IsEqualTo(result.Value.Id);
        await Assert.That(shoplists[0].Name).IsEqualTo("Groceries");
    }

    [Test]
    public async Task NullName_ShortCircuitsValidation_AndPersistsNothing()
    {
        var request = new CreateShoplist.Request(Name: null);

        var result = await SendAsync(request);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Validation);

        var shoplistsResult = await SendAsync(new GetShoplists.Request());
        await Assert.That(shoplistsResult.IsError).IsFalse();
        await Assert.That(shoplistsResult.Value.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Shoplist_IsNotVisibleToOtherUsers()
    {
        var otherUser = UserId.New();

        var createResult = await SendAsync(new CreateShoplist.Request("My list"));
        await Assert.That(createResult.IsError).IsFalse();

        var ownerResult = await SendAsync(new GetShoplists.Request());
        await Assert.That(ownerResult.IsError).IsFalse();
        await Assert.That(ownerResult.Value.Count).IsEqualTo(1);

        var otherResult = await SendAsync(new GetShoplists.Request(), asUser: otherUser);
        await Assert.That(otherResult.IsError).IsFalse();
        await Assert.That(otherResult.Value.Count).IsEqualTo(0);
    }
}
