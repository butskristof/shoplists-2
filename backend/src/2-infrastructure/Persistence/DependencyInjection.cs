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

        builder.Services.AddScoped<IAppDbContext, AppDbContext>();

        return builder;
    }
}
