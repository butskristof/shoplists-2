using Microsoft.EntityFrameworkCore;
using Shoplists.Domain.Models.Shoplists;

namespace Shoplists.Application.Common.Persistence;

public interface IAppDbContext
{
    DbSet<Shoplist> Shoplists { get; }

    IQueryable<Shoplist> CurrentUserShoplists();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
