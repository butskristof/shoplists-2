using Shoplists.Domain.Models.Users;

namespace Shoplists.Domain.Models.Shoplists;

public class Shoplist
{
    public ShoplistId Id { get; init; }

    public required string Name { get; set; }

    public required UserId OwnerId { get; init; }

    private readonly List<ShoplistItem> _items = [];
    public IReadOnlyList<ShoplistItem> Items => _items.AsReadOnly();
}
