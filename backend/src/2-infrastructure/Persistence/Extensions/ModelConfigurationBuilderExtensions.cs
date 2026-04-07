using Microsoft.EntityFrameworkCore;
using Shoplists.Domain.Models.ShoppingLists;

namespace Shoplists.Persistence.Extensions;

internal static class ModelConfigurationBuilderExtensions
{
    internal static ModelConfigurationBuilder ConfigureStronglyTypedIdConversions(
        this ModelConfigurationBuilder configurationBuilder
    )
    {
        configurationBuilder
            .Properties<ShoppingListId>()
            .HaveConversion<ShoppingListId.EfCoreValueConverter>();

        return configurationBuilder;
    }
}