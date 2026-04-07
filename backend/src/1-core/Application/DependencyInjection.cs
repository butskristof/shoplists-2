using Microsoft.Extensions.DependencyInjection;
using Shoplists.Application.Common.Pipeline;

namespace Shoplists.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.Assemblies = [typeof(DependencyInjection).Assembly];
            options.PipelineBehaviors = [typeof(LoggingBehavior<,>)];
            options.GenerateTypesAsInternal = true;
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        return services;
    }
}
