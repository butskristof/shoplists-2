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
        builder.AddNpgsqlDbContext<AppDbContext>(connectionName);

        // don't register as <IAppDbContext, AppDbContext> since it'll create an additional instance
        // reuse the already-registered one by getting it from the service provider
        builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return builder;
    }
}
