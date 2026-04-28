using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Tests.Common.Builders.Shoplists;

public sealed class ShoplistBuilder
{
    private ShoplistId _id = ShoplistId.New();
    private string _name = "Test Shoplist";
    private UserId _ownerId = new();

    public ShoplistBuilder WithId(ShoplistId id)
    {
        _id = id;
        return this;
    }

    public ShoplistBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ShoplistBuilder WithOwnerId(UserId ownerId)
    {
        _ownerId = ownerId;
        return this;
    }

    public Shoplist Build() =>
        new()
        {
            Id = _id,
            Name = _name,
            OwnerId = _ownerId,
        };

    public static implicit operator Shoplist(ShoplistBuilder builder) => builder.Build();
}
