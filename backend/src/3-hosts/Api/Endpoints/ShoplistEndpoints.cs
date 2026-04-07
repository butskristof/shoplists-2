using Mediator;
using Shoplists.Api.Extensions;
using Shoplists.Application.Features.Shoplists;

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
            .MapPost("/", CreateShoplist)
            .WithName(nameof(CreateShoplist))
            .Produces<CreateShoplist.Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        return app;
    }

    private static Task<IResult> GetShoplists(ISender sender) =>
        sender.Send(new GetShoplists.Request()).ToHttpResult();

    private static Task<IResult> CreateShoplist(ISender sender, CreateShoplist.Request request) =>
        sender
            .Send(request)
            .ToHttpResult(onSuccess: response =>
                TypedResults.Created($"/api/shoplists/{response.Id}", response)
            );
}
