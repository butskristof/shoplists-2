using Microsoft.Extensions.Hosting;

namespace Shoplists.Persistence;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddPersistence(
        this IHostApplicationBuilder builder,
        string connectionName
    )
    {
        builder.AddNpgsqlDbContext<AppDbContext>(connectionName);

        return builder;
    }
}
