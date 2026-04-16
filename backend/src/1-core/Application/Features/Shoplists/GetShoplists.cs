using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shoplists.Application.Common.Persistence;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Application.Features.Shoplists;

public static class GetShoplists
{
    public sealed record Request : IQuery<ErrorOr<IReadOnlyList<Response>>>;

    public sealed record Response(ShoplistId Id, string Name, Response.ItemCounts Items)
    {
        public sealed record ItemCounts(int Total, int Fulfilled);
    }

    internal sealed class Handler(ILogger<Handler> logger, IAppDbContext dbContext)
        : IQueryHandler<Request, ErrorOr<IReadOnlyList<Response>>>
    {
        public async ValueTask<ErrorOr<IReadOnlyList<Response>>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var shoplists = await dbContext
                .CurrentUserShoplists()
                .Select(s => new Response(
                    s.Id,
                    s.Name,
                    new Response.ItemCounts(s.Items.Count, s.Items.Count(i => i.IsFulfilled))
                ))
                .ToListAsync(cancellationToken);

            logger.LogDebug("Returning {Count} shoplists", shoplists.Count);

            return shoplists;
        }
    }
}
