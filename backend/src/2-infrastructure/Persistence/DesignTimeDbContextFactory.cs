using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shoplists.Persistence;

/// <summary>
/// Provides a <see cref="AppDbContext"/> instance for EF Core design-time tooling (e.g. <c>dotnet ef migrations add</c>).
///
/// At design time, <c>dotnet ef</c> needs to instantiate the DbContext to read the current model
/// and diff it against the last migration snapshot — but it never actually connects to a database.
/// The connection string here is a dummy that satisfies the Npgsql provider's requirement for a
/// valid-looking host; no real connection is established during migration scaffolding.
///
/// At runtime, Aspire injects the real connection string via <c>AddNpgsqlDbContext</c> in
/// <see cref="DependencyInjection.AddPersistence"/>, so this factory is never used.
///
/// Usage (from the Persistence project directory):
/// <code>
/// dotnet ef migrations add MigrationName
/// </code>
/// No <c>--startup-project</c> flag is needed because this factory lives in the same project
/// as the DbContext and migrations.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost")
            .Options;

        return new AppDbContext(options);
    }
}
