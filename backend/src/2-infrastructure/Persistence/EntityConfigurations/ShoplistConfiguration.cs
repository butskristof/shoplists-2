using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Persistence.Extensions;

namespace Shoplists.Persistence.EntityConfigurations;

internal sealed class ShoplistConfiguration : IEntityTypeConfiguration<Shoplist>
{
    public void Configure(EntityTypeBuilder<Shoplist> builder)
    {
        builder.HasKey(sl => sl.Id);
        builder.Property(sl => sl.Id).ValueGeneratedNever();

        builder.Property(sl => sl.Name).IsRequired();

        builder.Property(sl => sl.OwnerId).IsImmutableAfterInsert();
        builder.HasIndex(sl => sl.OwnerId);

        builder
            .HasMany(sl => sl.Items)
            .WithOne()
            .HasForeignKey(si => si.ShoplistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
