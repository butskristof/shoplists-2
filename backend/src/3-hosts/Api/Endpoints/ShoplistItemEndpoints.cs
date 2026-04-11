using Mediator;
using Shoplists.Api.Extensions;
using Shoplists.Application.Features.Shoplists.Items;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Api.Endpoints;

internal static class ShoplistItemEndpoints
{
    internal static IEndpointRouteBuilder MapShoplistItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/shoplists/{listId}/items").WithTags("Shoplist Items");

        group
            .MapPost("/", CreateShoplistItem)
            .WithName(nameof(CreateShoplistItem))
            .Produces<CreateShoplistItem.Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group
            .MapDelete("/{itemId}", DeleteShoplistItem)
            .WithName(nameof(DeleteShoplistItem))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group
            .MapPut("/{itemId}", UpdateShoplistItem)
            .WithName(nameof(UpdateShoplistItem))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group
            .MapPatch("/{itemId}/position", UpdateShoplistItemPosition)
            .WithName(nameof(UpdateShoplistItemPosition))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group
            .MapPatch("/{itemId}/checked", UpdateShoplistItemChecked)
            .WithName(nameof(UpdateShoplistItemChecked))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static Task<IResult> CreateShoplistItem(
        ISender sender,
        ShoplistId listId,
        CreateShoplistItem.Request request
    ) =>
        sender
            .Send(request with { ShoplistId = listId })
            .ToHttpResult(onSuccess: response =>
                TypedResults.Created($"/api/shoplists/{listId}", response)
            );

    private static Task<IResult> DeleteShoplistItem(
        ISender sender,
        ShoplistId listId,
        ShoplistItemId itemId
    ) =>
        sender
            .Send(new DeleteShoplistItem.Request(listId, itemId))
            .ToHttpResult(onSuccess: _ => TypedResults.NoContent());

    private static Task<IResult> UpdateShoplistItem(
        ISender sender,
        ShoplistId listId,
        ShoplistItemId itemId,
        UpdateShoplistItem.Request request
    ) =>
        sender
            .Send(request with { ShoplistId = listId, ItemId = itemId })
            .ToHttpResult(onSuccess: _ => TypedResults.NoContent());

    private static Task<IResult> UpdateShoplistItemPosition(
        ISender sender,
        ShoplistId listId,
        ShoplistItemId itemId,
        UpdateShoplistItemPosition.Request request
    ) =>
        sender
            .Send(request with { ShoplistId = listId, ItemId = itemId })
            .ToHttpResult(onSuccess: _ => TypedResults.NoContent());

    private static Task<IResult> UpdateShoplistItemChecked(
        ISender sender,
        ShoplistId listId,
        ShoplistItemId itemId,
        UpdateShoplistItemChecked.Request request
    ) =>
        sender
            .Send(request with { ShoplistId = listId, ItemId = itemId })
            .ToHttpResult(onSuccess: _ => TypedResults.NoContent());
}
