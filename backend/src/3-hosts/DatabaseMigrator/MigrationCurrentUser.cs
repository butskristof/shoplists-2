using Shoplists.Application.Common.Authentication;
using Shoplists.Domain.Models.Users;

namespace Shoplists.DatabaseMigrator;

/// <summary>
/// Dummy <see cref="ICurrentUser"/> for the migration host. Never called — the migrator
/// only applies schema migrations, it never executes application queries.
///
/// <see cref="Persistence.AppDbContext"/> requires <see cref="ICurrentUser"/> for
/// authorization-scoped queries. The DI container must be able to resolve it when
/// constructing the DbContext, even though <c>MigrateAsync()</c> never invokes
/// user-scoped query paths.
/// </summary>
internal sealed class MigrationCurrentUser : ICurrentUser
{
    public UserId UserId =>
        throw new InvalidOperationException("The database migrator does not have a current user.");
}
