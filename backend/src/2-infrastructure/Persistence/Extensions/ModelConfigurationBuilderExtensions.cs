using Microsoft.EntityFrameworkCore;
using Shoplists.Application.Common.Constants;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Persistence.Extensions;

internal static class ModelConfigurationBuilderExtensions
{
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
