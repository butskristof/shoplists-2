using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Shoplists.Domain.Models.Shoplists;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Api.OpenApi;

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
