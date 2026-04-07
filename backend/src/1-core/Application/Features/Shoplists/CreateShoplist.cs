using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Shoplists.Application.Common.Authentication;
using Shoplists.Application.Common.Persistence;
using Shoplists.Application.Common.Validation;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Application.Features.Shoplists;

public static class CreateShoplist
{
    public sealed record Request : ICommand<ErrorOr<Response>>
    {
        public string? Name { get; init; }
    }

    public sealed record Response(ShoplistId Id);

    internal sealed class Validator : BaseValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Name).ValidString(required: true);
        }
    }

    internal sealed class Handler(
        ILogger<Handler> logger,
        IAppDbContext dbContext,
        ICurrentUser currentUser
    ) : ICommandHandler<Request, ErrorOr<Response>>
    {
        public async ValueTask<ErrorOr<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var shoplist = new Shoplist { Name = request.Name!, OwnerId = currentUser.UserId };

            dbContext.Shoplists.Add(shoplist);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Created shoplist {ShoplistId}", shoplist.Id);

            return new Response(shoplist.Id);
        }
    }
}
