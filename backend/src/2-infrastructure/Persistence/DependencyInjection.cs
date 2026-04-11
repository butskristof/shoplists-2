using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shoplists.Application.Common.Persistence;

namespace Shoplists.Persistence;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddPersistence(
        this IHostApplicationBuilder builder,
        string connectionName
    )
    {
        // Register with AddDbContext (not AddDbContextPool) because AppDbContext has a scoped
        // constructor dependency (ICurrentUser). DbContext pooling uses a singleton pool that
        // cannot resolve scoped services.
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString(connectionName))
        );

        // Add Aspire's health check, retry, and telemetry on top of the manually registered context.
        builder.EnrichNpgsqlDbContext<AppDbContext>();

        // don't register as <IAppDbContext, AppDbContext> since it'll create an additional instance
        // reuse the already-registered one by getting it from the service provider
        builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return builder;
    }
}
