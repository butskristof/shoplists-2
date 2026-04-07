using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Shoplists.Persistence;

/// <summary>
/// Applies pending EF Core migrations to the database.
///
/// This class is the public entry point for running migrations while keeping
/// <see cref="AppDbContext"/> internal to the Persistence assembly. It is called
/// by the DatabaseMigrator host during Aspire orchestration.
///
/// ReSharper disable once InvalidXmlDocComment
/// <see cref="RelationalDatabaseFacadeExtensions.MigrateAsync"/> (EF Core 9+) handles:
/// <list type="bullet">
///   <item>Creating the database if it does not exist</item>
///   <item>Acquiring a distributed lock to prevent concurrent migration execution</item>
///   <item>Wrapping each migration in a transaction with the configured execution strategy</item>
///   <item>Checking the <c>__EFMigrationsHistory</c> table and applying only pending migrations</item>
///   <item>Exiting as a no-op if the database is already up to date</item>
/// </list>
///
/// There is no need to wrap the call in <c>CreateExecutionStrategy().ExecuteAsync()</c> or
/// an explicit transaction — EF Core 9+ manages both internally and will throw if you add
/// an outer transaction.
///
/// <c>EnsureCreatedAsync()</c> must NOT be called before <c>MigrateAsync()</c>. It bypasses
/// migrations entirely and creates the schema directly, which then causes <c>MigrateAsync()</c>
/// to fail.
/// </summary>
public static class DatabaseMigrationRunner
{
    public static async Task RunMigrationsAsync(
        IServiceProvider services,
        CancellationToken cancellationToken
    )
    {
        var scope = services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
