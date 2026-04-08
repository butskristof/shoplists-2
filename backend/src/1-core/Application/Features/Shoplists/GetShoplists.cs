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

    public sealed record Response(ShoplistId Id, string Name);

    internal sealed class Handler(ILogger<Handler> logger, IAppDbContext dbContext)
        : IQueryHandler<Request, ErrorOr<IReadOnlyList<Response>>>
    {
        public async ValueTask<ErrorOr<IReadOnlyList<Response>>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var shoplists = await dbContext
                .Shoplists.Select(s => new Response(s.Id, s.Name))
                .ToListAsync(cancellationToken);

            logger.LogDebug("Returning {Count} shoplists", shoplists.Count);

            return shoplists;
        }
    }
}
