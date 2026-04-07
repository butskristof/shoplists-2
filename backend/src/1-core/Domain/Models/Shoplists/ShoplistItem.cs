namespace Shoplists.Domain.Models.Shoplists;

public class ShoplistItem
{
    public ShoplistItemId Id { get; set; }

    public required string Name { get; set; }

    public bool IsChecked { get; set; } = false;

    public required int Position { get; set; }

    public required ShoplistId ShoplistId { get; set; }
}
