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

        return app;
    }

    private static Task<IResult> GetShoplists(ISender sender) =>
        sender.Send(new GetShoplists.Request()).ToHttpResult();
}
