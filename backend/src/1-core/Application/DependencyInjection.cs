using FluentValidation;
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
            options.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
            options.GenerateTypesAsInternal = true;
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddValidatorsFromAssemblyContaining(
            typeof(DependencyInjection),
            includeInternalTypes: true
        );

        return services;
    }
}
