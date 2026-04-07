using Mediator;
using Shoplists.Api.Extensions;
using Shoplists.Application.Features.Shoplists;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Api.Endpoints;

internal static class ShoplistEndpoints
{
    internal static IEndpointRouteBuilder MapShoplistEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/shoplists").WithTags("Shoplists");

        group
            .MapGet("/", GetShoplists)
            .WithName(nameof(GetShoplists))
            .Produces<IReadOnlyList<GetShoplists.Response>>();

        group
            .MapGet("/{id}", GetShoplist)
            .WithName(nameof(GetShoplist))
            .Produces<GetShoplist.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group
            .MapPost("/", CreateShoplist)
            .WithName(nameof(CreateShoplist))
            .Produces<CreateShoplist.Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group
            .MapPut("/{id}", UpdateShoplist)
            .WithName(nameof(UpdateShoplist))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group
            .MapDelete("/{id}", DeleteShoplist)
            .WithName(nameof(DeleteShoplist))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static Task<IResult> GetShoplists(ISender sender) =>
        sender.Send(new GetShoplists.Request()).ToHttpResult();

    private static Task<IResult> GetShoplist(ISender sender, ShoplistId id) =>
        sender.Send(new GetShoplist.Request { Id = id }).ToHttpResult();

    private static Task<IResult> CreateShoplist(ISender sender, CreateShoplist.Request request) =>
        sender
            .Send(request)
            .ToHttpResult(onSuccess: response =>
                TypedResults.Created($"/api/shoplists/{response.Id}", response)
            );

    private static Task<IResult> UpdateShoplist(
        ISender sender,
        ShoplistId id,
        UpdateShoplist.Request request
    ) =>
        sender
            .Send(request with { Id = id })
            .ToHttpResult(onSuccess: _ => TypedResults.NoContent());

    private static Task<IResult> DeleteShoplist(ISender sender, ShoplistId id) =>
        sender
            .Send(new DeleteShoplist.Request { Id = id })
            .ToHttpResult(onSuccess: _ => TypedResults.NoContent());
}
