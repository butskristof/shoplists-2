using Microsoft.EntityFrameworkCore;
using Shoplists.Application.Common.Constants;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Persistence.Extensions;

internal static class ModelConfigurationBuilderExtensions
{
    /// <summary>
    /// Configures value conversions for strongly-typed IDs used in the application.
    /// This method applies the appropriate type converters for strongly-typed IDs, such as
    /// <see cref="ShoplistId"/>, <see cref="ShoplistItemId"/>, and <see cref="UserId"/>,
    /// to ensure proper handling and storage in the database.
    /// </summary>
    /// <param name="configurationBuilder">
    /// The <see cref="ModelConfigurationBuilder"/> instance used to configure conventions for the application's models.
    /// </param>
    /// <returns>
    /// The updated <see cref="ModelConfigurationBuilder"/> instance with the strongly-typed ID conversions configured.
    /// </returns>
    internal static ModelConfigurationBuilder ConfigureStronglyTypedIdConversions(
        this ModelConfigurationBuilder configurationBuilder
    )
    {
        configurationBuilder
            .Properties<ShoplistId>()
            .HaveConversion<ShoplistId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<ShoplistItemId>()
            .HaveConversion<ShoplistItemId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<UserId>()
            .HaveConversion<UserId.EfCoreValueConverter>()
            .HaveMaxLength(ApplicationConstants.DefaultMaxStringLength);

        return configurationBuilder;
    }
}
