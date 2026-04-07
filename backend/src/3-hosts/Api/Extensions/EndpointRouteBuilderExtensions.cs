using Shoplists.Api.Endpoints;

namespace Shoplists.Api.Extensions;

internal static class EndpointRouteBuilderExtensions
{
    internal static IEndpointRouteBuilder MapShoplistsApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapShoplistEndpoints();

        return app;
    }
}
