using Shoplists.Domain.Models.Users;

namespace Shoplists.Domain.Models.Shoplists;

public class Shoplist
{
    public ShoplistId Id { get; init; } = ShoplistId.New();

    public required string Name { get; set; }

    public required UserId OwnerId { get; init; }

    private readonly List<ShoplistItem> _items = [];
    public IReadOnlyList<ShoplistItem> Items => _items.AsReadOnly();

    public ShoplistItem AddItem(string name)
    {
        var position = _items.Count > 0 ? _items.Max(i => i.Position) + 1 : 1;
        var item = new ShoplistItem
        {
            Name = name,
            Position = position,
            ShoplistId = Id,
        };
        _items.Add(item);
        return item;
    }

    public void RemoveItem(ShoplistItemId itemId)
    {
        var item =
            _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Item {itemId} not found in shoplist {Id}.");

        _items.Remove(item);
        foreach (var other in _items.Where(i => i.Position > item.Position))
            other.Position--;
    }

    public bool MoveItem(ShoplistItemId itemId, int newPosition)
    {
        var item =
            _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Item {itemId} not found in shoplist {Id}.");

        if (newPosition < 1 || newPosition > _items.Count)
            return false;

        var oldPosition = item.Position;
        if (oldPosition == newPosition)
            return true;

        if (oldPosition < newPosition)
        {
            foreach (
                var other in _items.Where(i =>
                    i.Position > oldPosition && i.Position <= newPosition
                )
            )
                other.Position--;
        }
        else
        {
            foreach (
                var other in _items.Where(i =>
                    i.Position >= newPosition && i.Position < oldPosition
                )
            )
                other.Position++;
        }

        item.Position = newPosition;
        return true;
    }
}
