namespace Shoplists.Domain.Models.Shoplists;

public class ShoplistItem
{
    public ShoplistItemId Id { get; init; }

    public required string Name { get; set; }

    public bool IsFulfilled { get; set; } = false;

    public required int Position { get; set; }

    public required ShoplistId ShoplistId { get; init; }
}
