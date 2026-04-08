using Microsoft.AspNetCore.OpenApi;
using Shoplists.Api.Authentication;
using Shoplists.Api.OpenApi;
using Shoplists.Application.Common.Authentication;

namespace Shoplists.Api.Extensions;

internal static class DependencyInjection
{
    internal static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddProblemDetails();

        services.AddScoped<ICurrentUser, FakeCurrentUser>();

        services.AddOpenApi(options =>
        {
            options.CreateSchemaReferenceId = typeInfo =>
            {
                var defaultId = OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
                if (defaultId is null)
                    return null;

                // Nested types (e.g. CreateShoplist.Response) get the default ID "Response",
                // which collides when multiple outer classes define the same nested type name.
                // Prefix with the declaring type name to match C# nested type notation.
                var declaringType = typeInfo.Type.DeclaringType;
                return declaringType is not null ? $"{declaringType.Name}.{defaultId}" : defaultId;
            };

            options.AddSchemaTransformer<StronglyTypedIdSchemaTransformer>();
        });
        return services;
    }
}
