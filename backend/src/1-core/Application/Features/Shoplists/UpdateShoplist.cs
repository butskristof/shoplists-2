using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shoplists.Application.Common.Persistence;
using Shoplists.Application.Common.Validation;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Application.Features.Shoplists;

public static class UpdateShoplist
{
    public sealed record Request : ICommand<ErrorOr<Success>>
    {
        public ShoplistId? Id { get; init; }
        public string? Name { get; init; }
    }

    internal sealed class Validator : BaseValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Id).NotNullWithErrorCode();
            RuleFor(r => r.Name).ValidString(required: true);
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
                .Shoplists.FirstOrDefaultAsync(s => s.Id == request.Id!.Value, cancellationToken)
                .ConfigureAwait(false);

            if (shoplist is null)
            {
                return Error.NotFound(description: "Shoplist not found.");
            }

            shoplist.Name = request.Name!;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Updated shoplist {ShoplistId}", shoplist.Id);

            return Result.Success;
        }
    }
}
