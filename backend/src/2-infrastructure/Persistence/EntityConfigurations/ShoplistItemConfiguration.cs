using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Persistence.Extensions;

namespace Shoplists.Persistence.EntityConfigurations;

internal sealed class ShoplistItemConfiguration : IEntityTypeConfiguration<ShoplistItem>
{
    public void Configure(EntityTypeBuilder<ShoplistItem> builder)
    {
        builder.HasKey(si => si.Id);
        builder.Property(si => si.Id).ValueGeneratedOnAdd();

        builder.Property(si => si.Name).IsRequired();

        builder.Property(si => si.ShoplistId).IsImmutableAfterInsert();
        builder.HasIndex(si => new { si.ShoplistId, si.Position }).IsUnique();
    }
}
