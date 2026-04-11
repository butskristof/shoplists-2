using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shoplists.Application.Common.Persistence;
using Shoplists.Application.Common.Validation;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Application.Features.Shoplists.Items;

public static class CreateShoplistItem
{
    public sealed record Request(ShoplistId? ShoplistId, string? Name)
        : ICommand<ErrorOr<Response>>;

    public sealed record Response(ShoplistItemId Id);

    internal sealed class Validator : BaseValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.ShoplistId).NotNullWithErrorCode();
            RuleFor(r => r.Name).ValidString(required: true);
        }
    }

    internal sealed class Handler(ILogger<Handler> logger, IAppDbContext dbContext)
        : ICommandHandler<Request, ErrorOr<Response>>
    {
        public async ValueTask<ErrorOr<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var shoplist = await dbContext
                .CurrentUserShoplists(includeItems: true)
                .FirstOrDefaultAsync(s => s.Id == request.ShoplistId!.Value, cancellationToken);

            if (shoplist is null)
                return Error.NotFound(description: "Shoplist not found.");

            var item = shoplist.AddItem(request.Name!);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogDebug(
                "Created item {ShoplistItemId} in shoplist {ShoplistId}",
                item.Id,
                shoplist.Id
            );

            return new Response(item.Id);
        }
    }
}
