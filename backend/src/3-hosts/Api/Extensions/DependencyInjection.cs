using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Shoplists.Api.Authentication;
using Shoplists.Api.OpenApi;
using Shoplists.Application.Common.Authentication;

namespace Shoplists.Api.Extensions;

internal static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddConfiguration(IConfiguration configuration)
        {
            services
                .AddOptions<AuthenticationSettings>()
                .Bind(configuration.GetSection(AuthenticationSettings.SectionName));

            return services;
        }

        internal IServiceCollection AddApi()
        {
            services.AddAuth();

            services.AddProblemDetails();
            services.AddOpenApi();

            return services;
        }

        private IServiceCollection AddAuth()
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var settings = scope.ServiceProvider.GetService<IOptions<AuthenticationSettings>>();
            if (settings is null)
                throw new InvalidOperationException(
                    $"Missing required configuration section '{AuthenticationSettings.SectionName}'."
                );

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = settings.Value.Authority;
                    options.Audience = settings.Value.Audience;
                    options.MapInboundClaims = false;
                });

            services.AddAuthorizationBuilder();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
            return services;
        }

        private IServiceCollection AddOpenApi()
        {
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
                    return declaringType is not null
                        ? $"{declaringType.Name}.{defaultId}"
                        : defaultId;
                };

                options.AddSchemaTransformer<StronglyTypedIdSchemaTransformer>();
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });
            return services;
        }
    }
}
