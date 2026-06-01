using ErrorOr;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Application.IntegrationTests.Common;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;
using Shoplists.Testing.Common.TestData;

namespace Shoplists.Application.IntegrationTests.Features.Shoplists;

public sealed class UpdateShoplistTests : IntegrationTestBase
{
    [Test]
    public async Task ValidRequest_UpdatesName()
    {
        var shoplistId = await CreateShoplistAsync("Groceries");

        var result = await SendAsync(new UpdateShoplist.Request(shoplistId, "Weekly groceries"));

        await Assert.That(result.IsError).IsFalse();
        var getResult = await SendAsync(new GetShoplist.Request(shoplistId));
        await Assert.That(getResult.Value.Name).IsEqualTo("Weekly groceries");
    }

    [Test]
    public async Task UnknownId_ReturnsNotFound()
    {
        var result = await SendAsync(new UpdateShoplist.Request(ShoplistId.New(), "New name"));

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task OtherUsersShoplist_ReturnsNotFound_AndLeavesItUnchanged()
    {
        var otherUser = UserId.New();
        var shoplistId = await CreateShoplistAsync("Their list", asUser: otherUser);

        var result = await SendAsync(new UpdateShoplist.Request(shoplistId, "Hacked"));

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
        var getResult = await SendAsync(new GetShoplist.Request(shoplistId), asUser: otherUser);
        await Assert.That(getResult.Value.Name).IsEqualTo("Their list");
    }
}
