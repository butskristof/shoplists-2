using Microsoft.EntityFrameworkCore;
using Shoplists.Application.Common.Constants;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Persistence.Extensions;

namespace Shoplists.Persistence;

internal sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options)
        : base(options) { }

    #region Entities

    public DbSet<Shoplist> Shoplists => Set<Shoplist>();

    #endregion

    #region Configuration

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // the base method is empty, but retain the call to minimise impact if
        // it should be used in a future version
        base.ConfigureConventions(configurationBuilder);

        // set text fields to have a reduced maximum length by default
        // this cuts down on a lot of varchar(max) columns, and can still be set to a higher
        // maximum length on a per-column basis
        configurationBuilder
            .Properties<string>()
            .HaveMaxLength(ApplicationConstants.DefaultMaxStringLength);

        // register strongly-typed ID value converters
        configurationBuilder.ConfigureStronglyTypedIdConversions();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // the base method is empty, but retain the call to minimise impact if
        // it should be used in a future version
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    #endregion
}
