using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shoplists.Application.Common.Persistence;
using Shoplists.Application.Common.Validation;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Application.Features.Shoplists.Items;

public static class UpdateShoplistItemFulfilled
{
    public sealed record Request(ShoplistId? ShoplistId, ShoplistItemId? ItemId, bool? IsFulfilled)
        : ICommand<ErrorOr<Success>>;

    internal sealed class Validator : BaseValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.ShoplistId).NotNullWithErrorCode();
            RuleFor(r => r.ItemId).NotNullWithErrorCode();
            RuleFor(r => r.IsFulfilled).NotNullWithErrorCode();
        }
    }

    internal sealed class Handler(ILogger<Handler> logger, IAppDbContext dbContext)
        : ICommandHandler<Request, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var shoplist = await dbContext
                .CurrentUserShoplists(includeItems: true)
                .FirstOrDefaultAsync(s => s.Id == request.ShoplistId!.Value, cancellationToken);

            if (shoplist is null)
                return Error.NotFound(description: "Shoplist not found.");

            var item = shoplist.Items.FirstOrDefault(i => i.Id == request.ItemId!.Value);
            if (item is null)
                return Error.NotFound(description: "Shoplist item not found.");

            item.IsFulfilled = request.IsFulfilled!.Value;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogDebug(
                "Set item {ShoplistItemId} fulfilled to {IsFulfilled} in shoplist {ShoplistId}",
                item.Id,
                request.IsFulfilled,
                shoplist.Id
            );

            return Result.Success;
        }
    }
}
