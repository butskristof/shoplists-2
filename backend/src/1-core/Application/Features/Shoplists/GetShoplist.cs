using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shoplists.Application.Common.Persistence;
using Shoplists.Application.Common.Validation;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Application.Features.Shoplists;

public static class GetShoplist
{
    public sealed record Request(ShoplistId? Id) : IQuery<ErrorOr<Response>>;

    public sealed record Response(ShoplistId Id, string Name, IReadOnlyList<ItemResponse> Items);

    public sealed record ItemResponse(ShoplistItemId Id, string Name, bool IsChecked, int Position);

    internal sealed class Validator : BaseValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Id).NotNullWithErrorCode();
        }
    }

    internal sealed class Handler(ILogger<Handler> logger, IAppDbContext dbContext)
        : IQueryHandler<Request, ErrorOr<Response>>
    {
        public async ValueTask<ErrorOr<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var shoplist = await dbContext
                .Shoplists.Where(s => s.Id == request.Id!.Value)
                .Select(s => new Response(
                    s.Id,
                    s.Name,
                    s.Items.OrderBy(i => i.Position)
                        .Select(i => new ItemResponse(i.Id, i.Name, i.IsChecked, i.Position))
                        .ToList()
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (shoplist is null)
                return Error.NotFound(description: "Shoplist not found.");

            logger.LogDebug("Returning shoplist {ShoplistId}", request.Id);

            return shoplist;
        }
    }
}
