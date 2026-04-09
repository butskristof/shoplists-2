using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shoplists.Application.Common.Persistence;
using Shoplists.Application.Common.Validation;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Application.Features.Shoplists;

public static class DeleteShoplist
{
    public sealed record Request(ShoplistId? Id) : ICommand<ErrorOr<Success>>;

    internal sealed class Validator : BaseValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Id).NotNullWithErrorCode();
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
                .CurrentUserShoplists()
                .FirstOrDefaultAsync(s => s.Id == request.Id!.Value, cancellationToken);

            if (shoplist is null)
                return Error.NotFound(description: "Shoplist not found.");

            dbContext.Shoplists.Remove(shoplist);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogDebug("Deleted shoplist {ShoplistId}", request.Id);

            return Result.Success;
        }
    }
}
