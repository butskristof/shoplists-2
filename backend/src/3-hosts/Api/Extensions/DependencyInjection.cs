using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Shoplists.Api.Authentication;
using Shoplists.Api.OpenApi;
using Shoplists.Application.Common.Authentication;

namespace Shoplists.Api.Extensions;

internal static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configure IOptions with settings for runtime use through dependency injection
        /// </summary>
        /// <param name="configuration">The configuration object used to provide application settings.</param>
        /// <returns>The updated IServiceCollection instance for chaining calls.</returns>
        internal IServiceCollection AddConfiguration(IConfiguration configuration)
        {
            // auth settings are only used once during setup, not necessary to expose them to the runtime
            // services
            //     .AddOptions<AuthenticationSettings>()
            //     .Bind(configuration.GetSection(AuthenticationSettings.SectionName));

            return services;
        }

        /// <summary>
        /// Configures API-related services, including authentication, authorization,
        /// problem details, and OpenAPI configuration, for runtime use through dependency injection.
        /// </summary>
        /// <param name="configuration">The configuration object used to provide application settings.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for chaining calls.</returns>
        internal IServiceCollection AddApi(IConfiguration configuration)
        {
            services.AddAuth(configuration);

            services.AddProblemDetails();
            services.AddOpenApi();

            return services;
        }

        /// <summary>
        /// Configures JWT-based authentication and authorization services,
        /// including setup for JwtBearer authentication, authorization policies,
        /// and current user handling through dependency injection.
        /// </summary>
        /// <param name="configuration">The configuration object used to provide authentication settings.</param>
        /// <returns>The updated IServiceCollection instance for chaining calls.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the required "Authentication" configuration section is missing.
        /// </exception>
        private IServiceCollection AddAuth(IConfiguration configuration)
        {
            var settings =
                configuration
                    .GetSection(AuthenticationSettings.SectionName)
                    .Get<AuthenticationSettings>()
                ?? throw new InvalidOperationException(
                    $"Missing required configuration section '{AuthenticationSettings.SectionName}'."
                );

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = settings.Authority;
                    options.Audience = settings.Audience;
                    options.MapInboundClaims = false;
                });

            services.AddAuthorizationBuilder();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
            return services;
        }

        /// <summary>
        /// Configures OpenAPI-related services for API documentation generation and customization
        /// using dependency injection.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for chaining calls.</returns>
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
