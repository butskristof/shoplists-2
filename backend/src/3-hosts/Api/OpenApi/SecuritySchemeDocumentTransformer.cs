using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Shoplists.Api.Authentication;

namespace Shoplists.Api.OpenApi;

/// <summary>
/// Registers Bearer and OAuth2 security schemes in the OpenAPI document components.
/// Per-operation security requirements are applied by <see cref="SecurityOperationTransformer"/>.
/// </summary>
internal sealed class SecuritySchemeDocumentTransformer(
    IOptions<AuthenticationSettings> authOptions
) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var settings = authOptions.Value;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>(
            StringComparer.Ordinal
        )
        {
            [SecuritySchemeNames.Bearer] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "Json Web Token",
            },
            [SecuritySchemeNames.OAuth2] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(settings.AuthorizationUrl!),
                        TokenUrl = new Uri(settings.TokenUrl!),
                        Scopes = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["openid"] = "OpenID Connect",
                            ["profile"] = "User profile",
                        },
                    },
                },
            },
        };

        return Task.CompletedTask;
    }
}
