using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Shoplists.Api.OpenApi;

internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>(
            StringComparer.Ordinal
        )
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "Json Web Token",
            },
        };

        document.Security ??= [];
        document.Security.Add(
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
            }
        );

        return Task.CompletedTask;
    }
}
