using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Api.OpenApi;

/// <summary>
/// A schema transformer for handling strongly-typed ID structures within OpenAPI schema definitions.
/// </summary>
/// <remarks>
/// This class customizes the OpenAPI schema by mapping these types to commonly understood formats and types,
/// such as UUIDs represented as strings, for better alignment with OpenAPI standards.
/// (default would be something like { "value": "[value here]" } instead of the value itself)
/// </remarks>
/// <example>
/// This transformer is typically used to process OpenAPI schemas for types such as <c>ShoplistId</c>,
/// <c>ShoplistItemId</c>, and <c>UserId</c>, ensuring they are correctly rendered in the OpenAPI documentation.
/// </example>
internal sealed class StronglyTypedIdSchemaTransformer : IOpenApiSchemaTransformer
{
    private static readonly Dictionary<
        Type,
        (JsonSchemaType Type, string? Format)
    > s_idTypeMappings = new()
    {
        [typeof(ShoplistId)] = (JsonSchemaType.String, "uuid"),
        [typeof(ShoplistItemId)] = (JsonSchemaType.String, "uuid"),
        [typeof(UserId)] = (JsonSchemaType.String, null),
    };

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        if (s_idTypeMappings.TryGetValue(context.JsonTypeInfo.Type, out var mapping))
        {
            schema.Properties?.Clear();
            schema.Type = mapping.Type;
            schema.Format = mapping.Format;
        }

        return Task.CompletedTask;
    }
}
