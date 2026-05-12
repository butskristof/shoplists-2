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
        builder.Property(si => si.Id).ValueGeneratedNever();

        builder.Property(si => si.Name).IsRequired();

        builder.Property(si => si.ShoplistId).IsImmutableAfterInsert();
        // Not unique: position is logically unique per shoplist (enforced by domain methods),
        // but EF Core's client-side command batching cannot resolve circular dependencies when
        // multiple items swap positions simultaneously. A deferred unique constraint in PostgreSQL
        // would work at the DB level, but EF Core throws before reaching the database.
        builder.HasIndex(si => new { si.ShoplistId, si.Position });
    }
}
