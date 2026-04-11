using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Shoplists.Api.OpenApi;

/// <summary>
/// Adds Bearer and OAuth2 security requirements to each operation unless the endpoint
/// is marked with <see cref="IAllowAnonymous"/>.
/// Assumes the security schemes are registered at the document level by
/// <see cref="SecuritySchemeDocumentTransformer"/>.
/// </summary>
internal sealed class SecurityOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var isAnonymous = context
            .Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>()
            .Any();

        if (isAnonymous)
            return Task.CompletedTask;

        operation.Security ??= [];

        operation.Security.Add(
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference(SecuritySchemeNames.Bearer, context.Document)
                ] = [],
            }
        );

        operation.Security.Add(
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference(SecuritySchemeNames.OAuth2, context.Document)
                ] = ["openid", "profile"],
            }
        );

        return Task.CompletedTask;
    }
}
