using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shoplists.Application.Common.Persistence;

namespace Shoplists.Persistence;

public static class DependencyInjection
{
    // Callers should always specify exactly one of connectionName or connectionString by name:
    //   builder.AddPersistence(connectionName: Resources.AppDb)        // production hosts
    //   builder.AddPersistence(connectionString: container.GetConnectionString())  // tests
    public static IHostApplicationBuilder AddPersistence(
        this IHostApplicationBuilder builder,
        string? connectionName = null,
        string? connectionString = null
    )
    {
        if ((connectionName is null) == (connectionString is null))
            throw new ArgumentException(
                "Provide exactly one of connectionName or connectionString.",
                nameof(connectionName)
            );

        // replace null connection string with value retrieved from configuration if not
        // passed in explicitly
        connectionString ??= builder.Configuration.GetConnectionString(connectionName!);
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{connectionName}' was not found in configuration."
            );

        // Register with AddDbContext (not AddDbContextPool) because AppDbContext has a scoped
        // constructor dependency (ICurrentUser). DbContext pooling uses a singleton pool that
        // cannot resolve scoped services.
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        // Add Aspire's health check, retry, and telemetry on top of the manually registered context.
        builder.EnrichNpgsqlDbContext<AppDbContext>();

        // don't register as <IAppDbContext, AppDbContext> since it'll create an additional instance
        // reuse the already-registered one by getting it from the service provider
        builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return builder;
    }
}
