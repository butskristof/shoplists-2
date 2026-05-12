using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shoplists.Persistence.Extensions;

internal static class PropertyBuilderExtensions
{
    /// <summary>
    /// Marks a property as immutable after the entity's initial insert.
    /// EF Core will throw if the property value is changed on a tracked entity
    /// during a subsequent <see cref="DbContext.SaveChangesAsync(System.Threading.CancellationToken)"/> call.
    ///
    /// Use this for non-key properties that should never change after creation
    /// (e.g., <c>OwnerId</c>, foreign keys to parent aggregates).
    /// Key properties already have this behavior by default.
    /// </summary>
    internal static PropertyBuilder<TProperty> IsImmutableAfterInsert<TProperty>(
        this PropertyBuilder<TProperty> builder
    )
    {
        builder.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        return builder;
    }
}
