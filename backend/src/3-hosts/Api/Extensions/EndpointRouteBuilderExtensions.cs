using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Shoplists.Api.Authentication;
using Shoplists.Api.Endpoints;
using Shoplists.Api.OpenApi;

namespace Shoplists.Api.Extensions;

internal static class EndpointRouteBuilderExtensions
{
    internal static IEndpointRouteBuilder MapShoplistsApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();

        api.MapShoplistEndpoints();
        api.MapShoplistItemEndpoints();

        return app;
    }

    internal static IEndpointRouteBuilder MapApiDocumentation(this IEndpointRouteBuilder app)
    {
        app.MapOpenApi(pattern: "/openapi/{documentName}.json");
        app.MapOpenApi(pattern: "/openapi/{documentName}.yaml");
        app.MapOpenApi(pattern: "/openapi/{documentName}.yml");

        var authSettings = app
            .ServiceProvider.GetRequiredService<IOptions<AuthenticationSettings>>()
            .Value;

        app.MapScalarApiReference(options =>
            options
                .AddPreferredSecuritySchemes(SecuritySchemeNames.OAuth2)
                .AddAuthorizationCodeFlow(
                    SecuritySchemeNames.OAuth2,
                    flow =>
                    {
                        flow.ClientId = authSettings.ClientId;
                        flow.ClientSecret = authSettings.ClientSecret;
                        flow.Pkce = Pkce.Sha256;
                        flow.SelectedScopes = ["openid", "profile"];
                    }
                )
        );

        return app;
    }
}
