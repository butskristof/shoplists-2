namespace Shoplists.Domain.Models.Shoplists;

public class ShoplistItem
{
    // only allow creating ShoplistItems via Shoplist, not directly via
    // the constructor from outside the domain model
    internal ShoplistItem() { }

    public ShoplistItemId Id { get; init; } = ShoplistItemId.New();

    public required string Name
    {
        get;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            field = value.Trim();
        }
    }

    public bool IsFulfilled { get; set; } = false;

    public required int Position { get; set; }

    public required ShoplistId ShoplistId { get; init; }
}
