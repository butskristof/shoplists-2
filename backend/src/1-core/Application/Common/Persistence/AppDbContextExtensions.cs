using Microsoft.EntityFrameworkCore;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Application.Common.Persistence;

internal static class AppDbContextExtensions
{
    internal static IQueryable<Shoplist> CurrentUserShoplists(
        this IAppDbContext dbContext,
        bool includeItems
    )
    {
        var query = dbContext.CurrentUserShoplists();

        if (includeItems)
            query = query.Include(s => s.Items);

        return query;
    }
}
